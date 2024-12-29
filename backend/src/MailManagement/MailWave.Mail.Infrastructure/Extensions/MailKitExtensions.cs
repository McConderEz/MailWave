using MailKit;
using MailKit.Search;
using MailWave.Mail.Domain.Entities;
using MimeKit;

namespace MailWave.Mail.Infrastructure.Extensions;

/// <summary>
/// Класс расширений для MailKit компонентов
/// </summary>
public static class MailKitExtensions
{
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
        
        var startPosition = Math.Max(0, uids.Count - (page * pageSize) + pageSize);
        var endPosition = Math.Max(0, uids.Count - (page * pageSize));

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
}