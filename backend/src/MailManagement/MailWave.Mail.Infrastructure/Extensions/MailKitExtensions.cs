using System.Net.Mail;
using System.Text;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using MimeKit;
using Attachment = System.Net.Mail.Attachment;
using Constraints = MailWave.Mail.Domain.Constraints.Constraints;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace MailWave.Mail.Infrastructure.Extensions;

/// <summary>
/// Класс расширений для MailKit компонентов
/// </summary>
public static class MailKitExtensions
{
    /// <summary>
    /// Порт для подключения к SMTP серверу
    /// </summary>
    private static readonly int _smtpPort = 587;
    
    /// <summary>
    /// Порт для подключения к IMAP серверу
    /// </summary>
    private static readonly int _imapPort = 993;


    /// <summary>
    /// Получение количества писем из папки
    /// </summary>
    /// <param name="folder">Папка</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество писем</returns>
    public static async Task<int> GetMessagesCountFromFolder(
        this IMailFolder folder,
        CancellationToken cancellationToken = default)
    {
        return (await folder.SearchAsync(
            SearchOptions.Count,
            SearchQuery.All,
            cancellationToken)).Count;
    }
    
    /// <summary>
    /// Получение всех сообщений из папки
    /// </summary>
    /// <param name="folder">Тип папки (Sent, Draft, Junk and etc.)</param>
    /// <param name="userName">Имя пользователя</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список писем из папки</returns>
    public static async Task<List<Letter>> GetMessagesAsync(
        this IMailFolder folder,
        string userName,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var uids = await folder.SearchAsync(SearchQuery.All, cancellationToken);
        
        var totalMessages = uids.Count;
        
        var startPosition = Math.Max(0, totalMessages - (page * pageSize));
        var endPosition = Math.Min(totalMessages, totalMessages - ((page - 1) * pageSize));
        
        var messages = new List<Letter>();

        var emailPrefix = GetMailDomain(userName);
        
        for (int i = startPosition; i < endPosition; i++)
        {
            var message = await folder.GetMessageAsync(uids[i], cancellationToken);
            messages.Add(new Letter
            {
                Id = uids[i].Id,
                From = message.From.Mailboxes.FirstOrDefault()!.Address,
                Body = message.HtmlBody,
                To = message.To.Select(t => t.ToString()).ToList(),
                Subject = message.Subject,
                Date = message.Date.UtcDateTime,
                Folder = folder.Name,
                IsCrypted = message.Subject.Contains(Constraints.CRYPTED_SUBJECT),
                IsSigned = message.Subject.Contains(Constraints.SIGNED_SUBJECT),
                EmailPrefix = emailPrefix
            });
        }

        return messages.OrderByDescending(m => m.Date).ToList();
    }

    /// <summary>
    /// Получение письма со всеми вложениями, если они существуют
    /// </summary>
    /// <param name="folder">Выбранная папка</param>
    /// <param name="userName">Имя пользователя</param>
    /// <param name="messageId">Уникальный идентификатор сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Письмо со всеми вложенными файлами</returns>
    public static async Task<Result<Letter>> GetMessageWithAttachmentsAsync(
        this IMailFolder folder,
        string userName,
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        UniqueId? uId = (await folder.SearchAsync(SearchQuery.All, cancellationToken))
            .FirstOrDefault(u => u.Id == messageId);

        if (uId is null)
            return Error.Failure("not.found",$"Cannot find letter with uid {messageId}");
            
        var message = await folder.GetMessageAsync(uId.Value, cancellationToken);

        if (message is null)
            return Error.Failure("get.message.error",$"Cannot get message");

        var emailPrefix = GetMailDomain(userName);
        
        var letter = new Letter
        {
            Id = uId.Value.Id,
            Body = message.HtmlBody,
            From = message.From.Mailboxes.FirstOrDefault()!.Address,
            To = message.To.Select(t => t.ToString()).ToList(),
            Subject = message.Subject,
            IsCrypted = message.Subject.Contains(Constraints.CRYPTED_SUBJECT),
            IsSigned = message.Subject.Contains(Constraints.SIGNED_SUBJECT),
            Date = message.Date.UtcDateTime,
            Folder = folder.Name,
            EmailPrefix = emailPrefix
        };
            
        if (message.Attachments.Any())
        {
            foreach (var attachment in message.Attachments)
            {
                var fileName = attachment.ContentDisposition.FileName;
                letter.AttachmentNames.Add(fileName);
            }
        }

        return letter;
    }

    /// <summary>
    /// Получение вложений письма
    /// </summary>
    /// <param name="folder">Выбранная папка</param>
    /// <param name="messageId">Уникальный идентификатор письма</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public static async Task<Result<List<Domain.Entities.Attachment>>> GetAttachmentOfMessage(
        this IMailFolder folder,
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        UniqueId? uId = (await folder.SearchAsync(SearchQuery.All, cancellationToken))
            .FirstOrDefault(u => u.Id == messageId);

        if (uId is null)
            return Error.Failure("not.found",$"Cannot find letter with uid {messageId}");
            
        var message = await folder.GetMessageAsync(uId.Value, cancellationToken);

        if (message is null)
            return Error.Failure("get.message.error",$"Cannot get message");

        List<Domain.Entities.Attachment> attachments = [];

        if (!message.Attachments.Any()) return attachments;
        
        foreach (var attachment in message.Attachments)
        {
            if (attachment is not MimePart part) continue;
            
            var stream = new MemoryStream();
            
            //TODO: разобраться тут
            //TODO: Грёбаный костыль
            //Так и живём...
            if (!attachment.ContentDisposition.FileName.EndsWith(".key") &&
                !attachment.ContentDisposition.FileName.EndsWith(".iv"))
            {
                await part.Content.DecodeToAsync(stream, cancellationToken);
                stream.Position = 0;
            }
            else
            {
                await part.Content.WriteToAsync(stream, cancellationToken);
                stream.Position = 0;
            }

            attachments.Add(new Domain.Entities.Attachment
            {
                FileName = attachment.ContentDisposition.FileName,
                Content = new MemoryStream(stream.ToArray())
            });
        }

        return attachments;
    }
    

    /// <summary>
    /// Подключение к smtp серверу с автоматическим подбором хоста и порта
    /// </summary>
    /// <param name="client">Объект smtp клиента</param>
    /// <param name="userName">Имя пользователя(почта)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public static async Task ConnectSmtpAsync(
        this SmtpClient client,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var connectionData = userName.Split("@")[1].Split(".");

        var host = "smtp" + "." + connectionData[0] + "." + connectionData[1];

        await client.ConnectAsync(host, _smtpPort, cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// Подключение к imap серверу с автоматическим подбором хоста и порта
    /// </summary>
    /// <param name="client">Объект imap клиента</param>
    /// <param name="userName">Имя пользователя(почта)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public static async Task ConnectImapAsync(
        this ImapClient client,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var connectionData = userName.Split("@")[1].Split(".");

        var host = "imap" + "." + connectionData[0] + "." + connectionData[1];

        await client.ConnectAsync(host, _imapPort, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Получение домена почты из userName
    /// </summary>
    /// <param name="userName">Имя пользователя</param>
    public static string GetMailDomain(string userName)
    {
        var connectionData = userName.Split("@")[1].Split(".");

        return connectionData[0];
    }
}