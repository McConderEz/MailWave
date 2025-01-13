using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailWave.Mail.Infrastructure.Extensions;
using MailWave.Mail.Infrastructure.Model;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Infrastructure.Dispatchers;

/// <summary>
/// Диспатчер для управления клиентами с открытыми соединениями
/// </summary>
public class MailClientDispatcher : IDisposable
{
    public readonly int MINUTES_OF_INACTIVE = 15;
    
    private readonly ILogger<MailClientDispatcher> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Dictionary<string, ImapClient> _imapClients = new();
    private readonly Dictionary<string, SmtpClient> _smtpClients = new();
    private readonly Dictionary<string, ClientSession> _clientSessions = new();
    
    public MailClientDispatcher(ILogger<MailClientDispatcher> logger, IDateTimeProvider dateTimeProvider)
    {
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Получить Imap клиент, если его нет - добавляем в словарь по email,
    /// открываем соединение и аутентифицируемся.
    /// </summary>
    /// <param name="email">Email адрес пользователя</param>
    /// <param name="password">Пароль к учётной записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<ImapClient> GetImapClientAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        if (!_imapClients.ContainsKey(email))
        {
            var client = new ImapClient();
            await client.ConnectImapAsync(email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(email, password, cancellationToken);
            _imapClients[email] = client;
            if (_clientSessions.ContainsKey(email))
            {
                _clientSessions[email].IsImapActive = true;
                _clientSessions[email].LastImapActivity = _dateTimeProvider.UtcNow;
            }
            else
                _clientSessions[email] = new ClientSession
                    { Email = email, LastImapActivity = _dateTimeProvider.UtcNow, IsImapActive = true };
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
    public async Task<SmtpClient> GetSmtpClientAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        if (!_smtpClients.ContainsKey(email))
        {
            var client = new SmtpClient();
            await client.ConnectSmtpAsync(email, cancellationToken: cancellationToken);
            await client.AuthenticateAsync(email, password, cancellationToken);
            _smtpClients[email] = client;
            if (_clientSessions.ContainsKey(email))
            {
                _clientSessions[email].IsSmtpActive = true;
                _clientSessions[email].LastSmtpActivity = _dateTimeProvider.UtcNow;
            }
            else
                _clientSessions[email] = new ClientSession
                    { Email = email, LastSmtpActivity = _dateTimeProvider.UtcNow, IsSmtpActive = true };
        }

        return _smtpClients[email];
    }

    /// <summary>
    /// Обновления активности клиента smtp
    /// </summary>
    /// <param name="email">Email пользователя</param>
    public void UpdateSmtpSessionActivity(string email)
    {
        if (_clientSessions.TryGetValue(email, out var session))
            session.LastSmtpActivity = _dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Обновления активности клиента imap
    /// </summary>
    /// <param name="email">Email пользователя</param>
    public void UpdateImapSessionActivity(string email)
    {
        if (_clientSessions.TryGetValue(email, out var session))
            session.LastImapActivity = _dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Проверка последней активности imap клиента
    /// </summary>
    /// <param name="email">Email пользователя</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    private bool IsClientImapInactive(string email)
    {
        var session = _clientSessions.TryGetValue(email, out var clientSession) ? clientSession : null;
        if (session is null)
            throw new ApplicationException("cannot get client session");

        return session.IsImapActive && session.LastImapActivity.AddMinutes(MINUTES_OF_INACTIVE) < DateTime.UtcNow;
    }
    
    /// <summary>
    /// Проверка последней активности smtp клиента
    /// </summary>
    /// <param name="email">Email пользователя</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    private bool IsClientSmtpInactive(string email)
    {
        var session = _clientSessions.TryGetValue(email, out var clientSession) ? clientSession : null;
        if (session is null)
            throw new ApplicationException("cannot get client session");

        return session.IsSmtpActive && session.LastSmtpActivity.AddMinutes(MINUTES_OF_INACTIVE) < DateTime.UtcNow;
    }
    
    /// <summary>
    /// Очистка неактивных клиентов
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task CleanupInactiveClientsAsync(CancellationToken cancellationToken)
    {
        foreach (var email in _imapClients.Keys.ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsClientImapInactive(email))
            {
                var client = _imapClients[email];
                var session = _clientSessions[email];
                session.IsImapActive = false;
                await client.DisconnectAsync(true, cancellationToken);
                client.Dispose();
                _imapClients.Remove(email);
                _logger.LogInformation($"Disconnected inactive IMAP client for {email}");
            }
        }

        foreach (var email in _smtpClients.Keys.ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsClientSmtpInactive(email))
            {
                var client = _smtpClients[email];
                var session = _clientSessions[email];
                session.IsSmtpActive = false;
                await client.DisconnectAsync(true, cancellationToken);
                client.Dispose();
                _smtpClients.Remove(email);
                _logger.LogInformation($"Disconnected inactive SMTP client for {email}");
            }
        }
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
