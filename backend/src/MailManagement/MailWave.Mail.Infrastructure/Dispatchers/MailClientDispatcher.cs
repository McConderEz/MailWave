using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailWave.Mail.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Infrastructure.Dispatchers;

/// <summary>
/// Диспатчер для управления клиентами с открытыми соединениями
/// </summary>
public class MailClientDispatcher : IDisposable
{
    private readonly ILogger<MailClientDispatcher> _logger;
    private readonly Dictionary<string, ImapClient> _imapClients = new();
    private readonly Dictionary<string, SmtpClient> _smtpClients = new();

    public MailClientDispatcher(ILogger<MailClientDispatcher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Получить Imap клиент, если его нет - добавляем в словарь по email,
    /// открываем соединение и аутентифицируемся.
    /// </summary>
    /// <param name="email">Email адрес пользователя</param>
    /// <param name="password">Пароль к учётной записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<ImapClient> GetImapClientAsync(string email, string password, CancellationToken cancellationToken)
    {
        if (!_imapClients.ContainsKey(email))
        {
            var client = new ImapClient();
            await client.ConnectImapAsync(email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(email, password, cancellationToken);
            _imapClients[email] = client;
        }

        return _imapClients[email];
    }

    /// <summary>
    /// Получить Smtp клиент, если его нет - добавляем в словарь по email,
    /// открываем соединение и аутентифицируемся.
    /// </summary>
    /// <param name="email">Email адрес пользователя</param>
    /// <param name="password">Пароль к учётной записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<SmtpClient> GetSmtpClientAsync(string email, string password, CancellationToken cancellationToken)
    {
        if (!_smtpClients.ContainsKey(email))
        {
            var client = new SmtpClient();
            await client.ConnectSmtpAsync(email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(email, password, cancellationToken);
            _smtpClients[email] = client;
        }

        return _smtpClients[email];
    }

    /// <summary>
    /// Закрытие соединения и освобождение ресурсов
    /// </summary>
    public void Dispose()
    {
        foreach (var client in _imapClients.Values)
        {
            client.Disconnect(true);
            client.Dispose();
        }

        foreach (var client in _smtpClients.Values)
        {
            client.Disconnect(true);
            client.Dispose();
        }
    }
}
