using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailWave.Core.DTOs;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using MimeKit;

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
    /// Получение всех сообщений из папки
    /// </summary>
    /// <param name="folder">Тип папки (Sent, Draft, Junk and etc.)</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список писем из папки</returns>
    public static async Task<List<Letter>> GetMessagesAsync(
        this IMailFolder folder,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var uids = await folder.SearchAsync(SearchQuery.All, cancellationToken);
        
        var startPosition = (page - 1) * pageSize;
        var endPosition = Math.Min(uids.Count, startPosition + pageSize);

        var messages = new List<Letter>();

        for (int i = startPosition; i < endPosition; i++)
        {
            var message = await folder.GetMessageAsync(uids[i], cancellationToken);
            messages.Add(new Letter
            {
                Id = uids[i].Id,
                From = message.From.ToString(),
                To = message.To.Select(t => t.Name).ToList(),
                Subject = message.Subject,
                Date = message.Date.DateTime
            });
        }

        return messages;
    }

    /// <summary>
    /// Получение письма со всеми вложениями, если они существуют
    /// </summary>
    /// <param name="folder">Выбранная папка</param>
    /// <param name="messageId">Уникальный идентификатор сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Письмо со всеми вложенными файлами</returns>
    public static async Task<Result<Letter>> GetMessageWithAttachmentsAsync(
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

        var letter = new Letter
        {
            Id = uId.Value.Id,
            Body = message.HtmlBody,
            From = message.From.ToString(),
            To = message.To.Select(t => t.Name).ToList(),
            Subject = message.Subject,
            Date = message.Date.DateTime
        };
            
        if (message.Attachments.Count() != 0)
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
}