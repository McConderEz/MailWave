using Hangfire;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailWave.Core.DTOs;
using MailWave.Mail.Contracts;
using MailWave.Mail.Domain.Entities;
using MailWave.Mail.Domain.Shared;
using MailWave.Mail.Infrastructure.Dispatchers;
using MailWave.Mail.Infrastructure.Extensions;
using MailWave.Mail.Infrastructure.Options;
using MailWave.Mail.Infrastructure.Repositories;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json;
using IMailService = MailWave.Mail.Application.MailService.IMailService;
using EmailFolder = MailWave.SharedKernel.Shared.Constraints.EmailFolder;

namespace MailWave.Mail.Infrastructure.Services;

/// <summary>
/// Сервис отправления сообщений по почте
/// </summary>
public class MailService : IMailService
{
    private readonly ILogger<MailService> _logger;
    private readonly LetterRepository _repository;
    private readonly UnitOfWork _unitOfWork;
    private readonly HybridCache _hybridCache;
    private readonly EmailValidator _validator;
    private readonly MailClientDispatcher _dispatcher;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    
    public MailService(
        ILogger<MailService> logger,
        EmailValidator validator,
        LetterRepository repository,
        UnitOfWork unitOfWork,
        HybridCache hybridCache,
        MailClientDispatcher dispatcher)
    {
        _logger = logger;
        _validator = validator;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _hybridCache = hybridCache;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Метод проверки соединения при входе в аккаунт
    /// </summary>
    /// <param name="userName">Имя учётной записи почты</param>
    /// <param name="password">Пароль учётной записи почты для сторонних ПО</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат: успешно/провал</returns>
    public async Task<Result> CheckConnection
        (string userName, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient();

            await client.ConnectSmtpAsync(userName, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(userName, password, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            
            return Result.Success();
        }
        catch
        {
            return Errors.MailErrors.ConnectionError();
        }
    }

    /// <summary>
    /// Метод отправки данных по почте с вложениями
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="letter">Письмо для отправки(адреса получателей, отправитель, основная информация)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> SendMessage(
        MailCredentialsDto mailCredentialsDto,
        IEnumerable<Attachment>? attachments,
        Letter letter,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Execute(letter.To);
        if (validationResult.IsFailure)
            return validationResult.Errors;

        letter.To = validationResult.Value;

        var mail = new MimeMessage();
        
        mail.From.Add(new MailboxAddress(mailCredentialsDto.Email, mailCredentialsDto.Email));

        foreach (var address in letter.To)
        {
            MailboxAddress.TryParse(address, out var mailAddress);
            mail.To.Add(mailAddress!);
        }

        var body = new BodyBuilder { HtmlBody = letter.Body };

        AddAttachments(attachments, body);
        
        mail.Body = body.ToMessageBody();
        mail.Subject = letter.Subject;
        
        try
        {
            var client = await _dispatcher.GetSmtpClientAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);
            
            await client.SendAsync(mail, cancellationToken);
            
            _dispatcher.UpdateSmtpSessionActivity(mailCredentialsDto.Email);
            
            foreach (var address in mail.To)
                _logger.LogInformation("Email successfully sent to {to}", address);
            
            return Result.Success();
        }
        catch 
        {
            _logger.LogError("The email message was not sent");
            return Error.Failure("send.email.error","The email message was not sent");
        }
    }

    /// <summary>
    /// Добавления вложений в тело письма
    /// </summary>
    /// <param name="attachments">Коллекция вложений</param>
    /// <param name="body">Тело письма</param>
    private void AddAttachments(IEnumerable<Attachment>? attachments, BodyBuilder body)
    {
        if (attachments is null)
            return;
        
        foreach (var attachment in attachments)
        {
            var mimePart = new MimePart("application/octet-stream")
            {
                Content = new MimeContent(attachment.Content),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                FileName = attachment.FileName
            };
                
            body.Attachments.Add(mimePart);
        }
    }

    /// <summary>
    /// Получения писем из папки
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="selectedFolder">Папка, из которой получаем</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Result со списком писем</returns>
    public async Task<Result<List<Letter>>> GetMessages(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder, 
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        try
        {
            
            var client = await _dispatcher.GetImapClientAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);
            
            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email ,client, cancellationToken);

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            
            var emailPrefix = MailKitExtensions.GetMailDomain(mailCredentialsDto.Email);
            
            var lettersCache = await _hybridCache.GetOrCreateAsync(
                $"letters_{selectedFolder.ToString()}_{page}_{emailPrefix}",
                async _ =>
                {
                    var lettersFromFolder = await folder.GetMessagesAsync(
                        mailCredentialsDto.Email, page, pageSize, cancellationToken);

                    return lettersFromFolder.Count == 0 ? [] : lettersFromFolder;
                },
                cancellationToken: cancellationToken);

            await folder.CloseAsync(true, cancellationToken);
            
            _dispatcher.UpdateImapSessionActivity(mailCredentialsDto.Email);
            
            _logger.LogInformation("Got all letters from folder {folder}", selectedFolder.ToString());
            
            return lettersCache;
        }
        catch
        {
            _logger.LogError("Cannot receive email message");

            return Error.Failure("email.receive.error","Cannot receive email message");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Метод для выбора папки с письмами
    /// </summary>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <param name="userName">Имя пользователя для определения констант папок</param>
    /// <param name="client">Imap клиент</param>
    /// <returns>Интерфейс IMailFolder с выбранной папкой</returns>
    private async Task<IMailFolder> SelectFolder(
        EmailFolder selectedFolder,
        string userName,
        ImapClient client,
        CancellationToken cancellationToken = default)
    {
        var mailFolderConstants = MailFolderConstants.Folder[userName.Split("@")[1].Split(".")[0]];
        
        var folder = await client.GetFolderAsync(selectedFolder switch
        { 
            EmailFolder.Sent => mailFolderConstants[1],
            EmailFolder.Drafts => mailFolderConstants[2],
            EmailFolder.Junk => mailFolderConstants[3],
            EmailFolder.Trash => mailFolderConstants[4],
            _ => mailFolderConstants[0]
        }, cancellationToken);
        
        return folder;
    }

    /// <summary>
    /// Получение письма по идентификатору
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Идентификатор письма uid</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Письмо с вложениями</returns>
    public async Task<Result<Letter>> GetMessage(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder, 
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _dispatcher.GetImapClientAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email, client, cancellationToken);

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var emailPrefix = MailKitExtensions.GetMailDomain(mailCredentialsDto.Email);
            
            var letterCache = await _hybridCache.GetOrCreateAsync(
                $"letters_{messageId}_{selectedFolder.ToString()}_{emailPrefix}",
                async _ =>
                {
                    var letterFromFolder = await folder
                        .GetMessageWithAttachmentsAsync(mailCredentialsDto.Email, messageId, cancellationToken);

                    if (letterFromFolder.IsFailure)
                        return null;
                    
                    return letterFromFolder.Value;
                },
                cancellationToken: cancellationToken);

            if (letterCache is null)
                return Errors.General.NotFound();
            
            await folder.CloseAsync(true, cancellationToken);
            
            _dispatcher.UpdateImapSessionActivity(mailCredentialsDto.Email);
            
            _logger.LogInformation("Got letter from folder {folder} with message id {messageId}",
                selectedFolder.ToString(), messageId);
            
            return letterCache;
        }
        catch (Exception ex)
        {
            _logger.LogError("Cannot get message with id {messageId}. Ex. message: {ex}", messageId, ex.Message);
            
            return Error.Failure("message.receive.error","Cannot get message");
        }
    }

    /// <summary>
    /// Получение вложений письма
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи пользователя</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Уникальный идентификатор сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result<List<Attachment>>> GetAttachmentsOfMessage(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder,
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _dispatcher.GetImapClientAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email, client, cancellationToken);

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var attachments = await folder.GetAttachmentOfMessage(messageId, cancellationToken);

            if (attachments.IsFailure)
                return attachments.Errors;
            
            await folder.CloseAsync(true, cancellationToken);
            
            _dispatcher.UpdateImapSessionActivity(mailCredentialsDto.Email);
            
            _logger.LogInformation("Got attachments of message from folder {folder} with message id {messageId}",
                selectedFolder.ToString(), messageId);
            
            return attachments;
        }
        catch (Exception ex)
        {
            _logger.LogError("Cannot get attachments of message with id {messageId}. Ex. message: {ex}",
                messageId, ex.Message);
            
            return Error.Failure("message.receive.error","Cannot get message");
        }
    }

    /// <summary>
    /// Получение общего количества писем из папки
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи пользователя</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result<int>> GetMessagesCountFromFolder(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _dispatcher.GetImapClientAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email, client, cancellationToken);
            
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            
            var totalMessagesCount = await folder.GetMessagesCountFromFolder(cancellationToken);
            
            await folder.CloseAsync(true, cancellationToken);
            
            _dispatcher.UpdateImapSessionActivity(mailCredentialsDto.Email);
            
            _logger.LogInformation("Got messages count from folder {folder}: {messageCount}",
                selectedFolder.ToString(), totalMessagesCount);
            
            return totalMessagesCount;
        }
        catch (Exception ex)
        {
            _logger.LogError("Cannot get messages count from folder");
            
            return Error.Failure("message.receive.error","Cannot get message");
        }
    }

    /// <summary>
    /// Отправка запланированного сообщения
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="letter">Письмо для отправки(адреса получателей, отправитель, основная информация)</param>
    /// <param name="enqueueAt">Дата и время отправки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public Task<Result> SendScheduledMessage(
        MailCredentialsDto mailCredentialsDto,
        IEnumerable<Attachment>? attachments,
        Letter letter,
        DateTime enqueueAt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduling message to be sent at: {enqueueAt}", enqueueAt);
        
        var attachmentsJson = attachments is not null ? JsonConvert.SerializeObject(attachments) : null;
        
        string jobId = BackgroundJob.Schedule(
            () => DecoratorSendScheduledMessage(mailCredentialsDto, attachmentsJson, letter, cancellationToken),
            enqueueAt.AddHours(-3));

        _logger.LogInformation("Scheduled message with job ID: {jobId}", jobId);

        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Метод-обёртка для отправки запланированного сообщения с вложениями через hangfire
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="attachmentsJson">Вложения в формате json</param>
    /// <param name="letter">Письмо для отправки(адреса получателей, отправитель, основная информация)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> DecoratorSendScheduledMessage(
        MailCredentialsDto mailCredentialsDto,
        string? attachmentsJson,
        Letter letter,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Attachment>? attachments = 
            attachmentsJson is not null ? JsonConvert.DeserializeObject<IEnumerable<Attachment>>(attachmentsJson) : null;

        var result = await SendMessage(mailCredentialsDto, attachments, letter, cancellationToken);
        if (result.IsFailure)
            return result.Errors;
        
        return Result.Success();
    }
    
    /// <summary>
    /// Сохранение писем в базу данных
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="messageIds">Коллекция уникальных идентификаторов</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> SaveMessagesInDataBase(
        MailCredentialsDto mailCredentialsDto,
        IEnumerable<uint> messageIds,
        EmailFolder selectedFolder,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _unitOfWork.BeginTransaction(cancellationToken);
        try
        {
            var client = await _dispatcher.GetImapClientAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email, client, cancellationToken);

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var emailPrefix = MailKitExtensions.GetMailDomain(mailCredentialsDto.Email);
            
            foreach (var messageId in messageIds)
            {
                var letter =
                    await folder.GetMessageWithAttachmentsAsync(mailCredentialsDto.Email, messageId, cancellationToken);

                if (letter.IsFailure)
                    return letter.Errors;

                if (letter.Value.IsCrypted)
                {
                    /*var result = await _decryptMessageContract.GetDecryptedLetter(
                        mailCredentialsDto,
                        selectedFolder,
                        messageId,
                        cancellationToken);
                    
                    if(result.IsFailure)
                        return result.Errors;
                    
                    letter.Value.Body = result.Value.Body;*/
                }
                
                var isExist = await _repository
                    .GetById(folder.Name, messageId, emailPrefix, cancellationToken);
                
                if (isExist.IsFailure)
                    await _repository.Add([letter.Value], cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            transaction.Commit();
            
            _logger.LogInformation("batch of letters was saved in database");

            return Result.Success();
        }
        catch(Exception ex)
        {
            transaction.Rollback();   
            
            _logger.LogError("Fail to save letters to database.Ex. message: {ex} ", ex.Message);

            return Error.Failure("save.db.fail", "Cannot save letters in db");
        }
    }

    /// <summary>
    /// Удаление письма из выбранной папки по уникальному идентификатору
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Идентификатор письма uid</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> DeleteMessage(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder,
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _unitOfWork.BeginTransaction(cancellationToken);
        
        try
        {
            var client = await _dispatcher.GetImapClientAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder,mailCredentialsDto.Email, client, cancellationToken);
            
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            UniqueId? uId = (await folder.SearchAsync(SearchQuery.All, cancellationToken))
                .FirstOrDefault(u => u.Id == messageId);

            if (uId is null)
                return Errors.General.NotFound();

            var emailPrefix = MailKitExtensions.GetMailDomain(mailCredentialsDto.Email);
            
            await _repository.Delete(folder.Name, messageId, emailPrefix, cancellationToken);

            await _hybridCache.RemoveAsync($"letters_{messageId}_{selectedFolder.ToString()}_{emailPrefix}",
                cancellationToken);

            var result = await folder.StoreAsync(
                uId.Value,
                new StoreFlagsRequest(StoreAction.Add, MessageFlags.Deleted) { Silent = true },
                cancellationToken);

            if (!result)
                return Error.Failure("delete.mark.error","Cannot marked message as deleted");

            await folder.ExpungeAsync(cancellationToken);
            await folder.CloseAsync(true, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            transaction.Commit();
            
            _dispatcher.UpdateImapSessionActivity(mailCredentialsDto.Email);
            
            _logger.LogInformation("Marked message was deleted");

            return Result.Success();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            
            _logger.LogError("Cannot deleted message. Ex. message: {ex}", ex.Message);
            return Error.Failure("delete.message.error","Cannot marked message as deleted");
        }
    }

    /// <summary>
    /// Перемещение письма из выбранной папки в целевую по уникальному идентификатору 
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="targetFolder">Папка для перемещения</param>
    /// <param name="messageId">Идентификатор письма</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> MoveMessage(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder,
        EmailFolder targetFolder,
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _dispatcher.GetImapClientAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email, client, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            
            UniqueId? uId = (await folder.SearchAsync(SearchQuery.All, cancellationToken))
                .FirstOrDefault(u => u.Id == messageId);
            
            var folderForMove = await SelectFolder(targetFolder, mailCredentialsDto.Email, client, cancellationToken);

            if (uId is null)
                return Errors.General.NotFound();

            var emailPrefix = MailKitExtensions.GetMailDomain(mailCredentialsDto.Email);
            
            await _repository.Delete(folder.Name, messageId, emailPrefix, cancellationToken);
            
            await folder.MoveToAsync(uId.Value, folderForMove, cancellationToken);

            await folder.CloseAsync(true, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _dispatcher.UpdateImapSessionActivity(mailCredentialsDto.Email);
            
            _logger.LogInformation(
                "Message with id {messageId} was moved from folder {previousFolder} to folder {targetFolder}",
                messageId, selectedFolder.ToString(), targetFolder.ToString());

            return Result.Success();
        }
        catch(Exception ex)
        {
            _logger.LogError("Cannot move message. Ex. message: {ex}", ex.Message);
            return Error.Failure("message.move.error","Cannot move message");
        }
    }
}