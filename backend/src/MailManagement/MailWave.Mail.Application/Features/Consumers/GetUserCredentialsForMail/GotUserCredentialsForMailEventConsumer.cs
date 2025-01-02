using MailWave.Accounts.Contracts.Messaging;
using MailWave.Core.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Consumers.GetUserCredentialsForMail;

public class GotUserCredentialsForMailEventConsumer: IConsumer<GotUserCredentialsForMailEvent>
{
    private readonly MailCredentialsScopedData _mailCredentials;
    private readonly ILogger<GotUserCredentialsForMailEventConsumer> _logger;

    public GotUserCredentialsForMailEventConsumer(
        MailCredentialsScopedData mailCredentials,
        ILogger<GotUserCredentialsForMailEventConsumer> logger)
    {
        _mailCredentials = mailCredentials;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<GotUserCredentialsForMailEvent> context)
    {
        var message = context.Message;

        _mailCredentials.Email = message.Email;
        _mailCredentials.Password = message.Password;
        
        _logger.LogInformation("Mail credentials are set");
        
        return Task.CompletedTask;
    }
}