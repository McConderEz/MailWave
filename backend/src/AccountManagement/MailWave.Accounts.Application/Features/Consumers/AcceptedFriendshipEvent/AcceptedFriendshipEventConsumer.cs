using MailWave.Accounts.Application.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace MailWave.Accounts.Application.Features.Consumers.AcceptedFriendshipEvent;

public class AcceptedFriendshipEventConsumer: IConsumer<Mail.Contracts.Messaging.AcceptedFriendshipEvent>
{
    private readonly ILogger<AcceptedFriendshipEventConsumer> _logger;
    private readonly IFriendshipRepository _repository;

    public AcceptedFriendshipEventConsumer(
        ILogger<AcceptedFriendshipEventConsumer> logger,
        IFriendshipRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<Mail.Contracts.Messaging.AcceptedFriendshipEvent> context)
    {
        var message = context.Message;

        var friendship = await _repository.GetByEmails(
            message.FirstUserEmail,
            message.SecondUserEmail,
            context.CancellationToken);

        if (friendship is null)
            throw new Exception("Friendship was not sent or friendship is not exist");

        if (friendship.IsAccepted)
            throw new Exception("Friendship already accepted");
        
        var updateSettings = new BsonDocument("$set", new BsonDocument("IsAccepted", true));

        await _repository.Update(friendship.Id, updateSettings, context.CancellationToken);

        _logger.LogInformation("Consumer: User {first} accepted friendship with {second}",
            message.FirstUserEmail, message.SecondUserEmail);
    }
}