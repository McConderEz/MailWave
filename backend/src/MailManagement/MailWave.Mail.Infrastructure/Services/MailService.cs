using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailWave.Mail.Domain.Entities;
using MailWave.Mail.Domain.Shared;
using MailWave.Mail.Infrastructure.Extensions;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Logging;
using MimeKit;
using IMailService = MailWave.Mail.Application.MailService.IMailService;
using EmailFolder = MailWave.Mail.Domain.Constraints.Constraints.EmailFolder;

namespace MailWave.Mail.Infrastructure.Services;

/// <summary>
/// Сервис отправления сообщений по почте
/// </summary>
public class MailService : IMailService
{
   // private readonly MailOptions _mailOptions;
    private readonly ILogger<MailService> _logger;
    private readonly EmailValidator _validator;

    //TODO: Все письма при получении пока что не проставляют false/true в IsCrypted/IsSigned
    
    public MailService(
        //IOptions<MailOptions> mailOptions,
        ILogger<MailService> logger,
        EmailValidator validator)
    {
        //_mailOptions = mailOptions.Value;
        _logger = logger;
        _validator = validator;
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

            await client.ConnectAsync("smtp.gmail.com", 587, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(userName, password, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            
            return Result.Success();
        }
        catch(Exception ex)
        {
            return Errors.MailErrors.ConnectionError();
        }
    }

    /// <summary>
    /// Метод отправки данных по почте
    /// </summary>
    /// <param name="letter">Письмо для отправки(адреса получателей, отправитель, основная информация)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> SendMessage(Letter letter, CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Execute(letter.To);
        if (validationResult.IsFailure)
            return validationResult.Errors;

        letter.To = validationResult.Value;

        var mail = new MimeMessage();
        
        //TODO: Отредачить
        mail.From.Add(new MailboxAddress("minoddein.ezz@gmail.com", "minoddein.ezz@gmail.com"));

        foreach (var address in letter.To)
        {
            MailboxAddress.TryParse(address, out var mailAddress);
            mail.To.Add(mailAddress!);
        }

        var body = new BodyBuilder { HtmlBody = letter.Body };

        mail.Body = body.ToMessageBody();
        mail.Subject = letter.Subject;

        try
        {
            using var client = new SmtpClient();

            //TODO: Создать хранение настроек
            await client.ConnectAsync("smtp.gmail.com", 587, cancellationToken: cancellationToken);
            await client.AuthenticateAsync("minoddein.ezz@gmail.com", "urlruiukmyuarruj", cancellationToken);
            await client.SendAsync(mail, cancellationToken);
            await client.DisconnectAsync(true,cancellationToken);
            
            foreach (var address in mail.To)
                _logger.LogInformation("Email successfully sended to {to}", address);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("The email message was not sent");
            return Error.Failure("send.email.error","The email message was not sent");
        }
    }
    
    //TODO: Добавить кэширование

    /// <summary>
    /// Получения писем из папки
    /// </summary>
    /// <param name="selectedFolder">Папка, из которой получаем</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Result со списком писем</returns>
    public async Task<Result<List<Letter>>> GetMessages(
        EmailFolder selectedFolder, 
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new ImapClient();

            await client.ConnectAsync("", 0, cancellationToken: cancellationToken);
            await client.AuthenticateAsync("", "", cancellationToken);

            var folder = await SelectFolder(selectedFolder, client, cancellationToken);

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var result = await folder.GetMessagesAsync(page, pageSize, cancellationToken);
            
            await client.DisconnectAsync(true,cancellationToken);
            
            _logger.LogInformation("Got all letters from folder {folder}", selectedFolder.ToString());
            
            return result;
        }
        catch (Exception ex)
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
    /// <param name="client">Imap клиент</param>
    /// <returns>Интерфейс IMailFolder с выбранной папкой</returns>
    private async Task<IMailFolder> SelectFolder(
        EmailFolder selectedFolder,
        ImapClient client,
        CancellationToken cancellationToken = default)
    {
        var folder = await client.GetFolderAsync(selectedFolder switch
        { 
            EmailFolder.Sent => SpecialFolder.Sent.ToString(),
            EmailFolder.Drafts => SpecialFolder.Drafts.ToString(),
            EmailFolder.Junk => SpecialFolder.Junk.ToString(),
            EmailFolder.Trash => SpecialFolder.Trash.ToString(),
            _ => client.Inbox.ToString()
        }, cancellationToken);
        return folder;
    }

    /// <summary>
    /// Получение письма по идентификатору
    /// </summary>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Идентификатор письма uid</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Письмо с вложениями</returns>
    public async Task<Result<Letter>> GetMessage(
        EmailFolder selectedFolder, 
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new ImapClient();

            await client.ConnectAsync("", 0, cancellationToken: cancellationToken);
            await client.AuthenticateAsync("", "", cancellationToken);

            var folder = await SelectFolder(selectedFolder, client, cancellationToken);

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
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Идентификатор письма uid</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> DeleteMessage(
        EmailFolder selectedFolder,
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new ImapClient();
            
            await client.ConnectAsync("", 0, cancellationToken: cancellationToken);
            await client.AuthenticateAsync("", "", cancellationToken);

            var folder = await SelectFolder(selectedFolder, client, cancellationToken);
            
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
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="targetFolder">Папка для перемещения</param>
    /// <param name="messageId">Идентификатор письма</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> MoveMessage(EmailFolder selectedFolder, EmailFolder targetFolder, uint messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new ImapClient();
            
            await client.ConnectAsync("", 0, cancellationToken: cancellationToken);
            await client.AuthenticateAsync("", "", cancellationToken);

            var folder = await SelectFolder(selectedFolder, client, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            
            var folderForMove = await SelectFolder(targetFolder, client, cancellationToken);
            await folderForMove.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            UniqueId? uId = (await folder.SearchAsync(SearchQuery.All, cancellationToken))
                .FirstOrDefault(u => u.Id == messageId);

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