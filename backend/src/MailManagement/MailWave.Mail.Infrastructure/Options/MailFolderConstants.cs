namespace MailWave.Mail.Infrastructure.Options;

/// <summary>
/// Константы с названиями папок для каждой почты
/// </summary>
public static class MailFolderConstants
{
    public static Dictionary<string, List<string>> Folder = new()
    {
        { "mail", ["INBOX", "Отправленные", "Черновики", "Спам", "Корзина"] },
        { "yandex", ["INBOX", "Sent", "Drafts", "Spam", "Trash"] },
        { "gmail", ["INBOX", "[Gmail]/Отправленные", "[Gmail]/Черновики", "[Gmail]/Спам", "[Gmail]/Корзина"] }
    };
}