using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailWave.Core.DTOs;
using MailWave.Mail.Domain.Entities;
using MailWave.Mail.Domain.Shared;
using MailWave.Mail.Infrastructure.Extensions;
using MailWave.Mail.Infrastructure.Options;
using MailWave.Mail.Infrastructure.Repositories;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using MimeKit;
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

    //TODO: Все письма при получении пока что не проставляют false/true в IsCrypted/IsSigned
    
    public MailService(
        ILogger<MailService> logger,
        EmailValidator validator,
        LetterRepository repository,
        UnitOfWork unitOfWork,
        HybridCache hybridCache)
    {
        _logger = logger;
        _validator = validator;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _hybridCache = hybridCache;
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
            using var client = new SmtpClient();
            
            await client.ConnectSmtpAsync(mailCredentialsDto.Email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);
            await client.SendAsync(mail, cancellationToken);
            await client.DisconnectAsync(true,cancellationToken);
            
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
        if (attachments is not null)
        {
            foreach (var attachment in attachments)
            {
                var mimePart = new MimePart("application/octet-stream")
                {
                    Content = new MimeContent(attachment.Content),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = attachment.FileName
                };
                
                body.Attachments.Add(mimePart);
            }
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
        try
        {
            using var client = new ImapClient();

            await client.ConnectImapAsync(mailCredentialsDto.Email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email ,client, cancellationToken);

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var result = await folder.GetMessagesAsync(page, pageSize, cancellationToken);
            
            await client.DisconnectAsync(true,cancellationToken);
            
            _logger.LogInformation("Got all letters from folder {folder}", selectedFolder.ToString());
            
            return result;
        }
        catch
        {
            _logger.LogError("Cannot receive email message");

            return Error.Failure("email.receive.error","Cannot receive email message");
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
            using var client = new ImapClient();

            await client.ConnectImapAsync(mailCredentialsDto.Email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email, client, cancellationToken);

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            
            var letter = await folder.GetMessageWithAttachmentsAsync(messageId, cancellationToken);

            await client.DisconnectAsync(true, cancellationToken);
            
            if (letter.IsFailure)
                return letter.Errors;
            
            _logger.LogInformation("Got letter from folder {folder} with message id {messageId}",
                selectedFolder.ToString(), messageId);
            
            return letter;
        }
        catch (Exception ex)
        {
            _logger.LogError("Cannot get message with id {messageId}. Ex. message: {ex}", messageId, ex.Message);
            return Error.Failure("message.receive.error","Cannot get message");
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
        try
        {
            using var client = new ImapClient();
            
            await client.ConnectImapAsync(mailCredentialsDto.Email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder,mailCredentialsDto.Email, client, cancellationToken);
            
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            UniqueId? uId = (await folder.SearchAsync(SearchQuery.All, cancellationToken))
                .FirstOrDefault(u => u.Id == messageId);

            if (uId is null)
                return Errors.General.NotFound();

            var result = await folder.StoreAsync(
                uId.Value,
                new StoreFlagsRequest(StoreAction.Add, MessageFlags.Deleted) { Silent = true },
                cancellationToken);

            if (!result)
                return Error.Failure("delete.mark.error","Cannot marked message as deleted");

            await folder.ExpungeAsync(cancellationToken);
            
            _logger.LogInformation("Marked message was deleted");

            return Result.Success();
        }
        catch (Exception ex)
        {
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
            using var client = new ImapClient();
            
            await client.ConnectImapAsync(mailCredentialsDto.Email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(
                mailCredentialsDto.Email, mailCredentialsDto.Password, cancellationToken);

            var folder = await SelectFolder(selectedFolder, mailCredentialsDto.Email, client, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            
            UniqueId? uId = (await folder.SearchAsync(SearchQuery.All, cancellationToken))
                .FirstOrDefault(u => u.Id == messageId);
            
            var folderForMove = await SelectFolder(targetFolder, mailCredentialsDto.Email, client, cancellationToken);

            if (uId is null)
                return Errors.General.NotFound();

            await folder.MoveToAsync(uId.Value, folderForMove, cancellationToken);
            
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