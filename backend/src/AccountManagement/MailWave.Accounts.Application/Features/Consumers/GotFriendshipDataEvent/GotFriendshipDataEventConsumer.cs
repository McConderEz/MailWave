using MailWave.Accounts.Application.Repositories;
using MailWave.Accounts.Domain.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace MailWave.Accounts.Application.Features.Consumers.GotFriendshipDataEvent;

/// <summary>
/// Добавления содружества в БД
/// </summary>
public class GotFriendshipDataEventConsumer: IConsumer<Mail.Contracts.Messaging.GotFriendshipDataEvent>
{
    private readonly ILogger<GotFriendshipDataEventConsumer> _logger;
    private readonly IFriendshipRepository _repository;

    public GotFriendshipDataEventConsumer(
        ILogger<GotFriendshipDataEventConsumer> logger,
        IFriendshipRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<Mail.Contracts.Messaging.GotFriendshipDataEvent> context)
    {
        var message = context.Message;
        
        var friendship = await _repository.GetByEmails(message.FirstEmail, message.SecondEmail);
        if (friendship is not null)
            throw new Exception("Friendship already exist");

        friendship = new Friendship
        {
            Id = Guid.NewGuid().ToString(),
            FirstUserId = Guid.NewGuid().ToString(),
            FirstUserEmail = message.FirstEmail,
            SecondUserId = Guid.NewGuid().ToString(),
            SecondUserEmail = message.SecondEmail,
            PublicKey = message.PublicKey,
            PrivateKey = message.PrivateKey
        };

        await _repository.Add(friendship);
        
        _logger.LogInformation("Added friendship {first} and {second}", message.FirstEmail, message.SecondEmail);
    }
}