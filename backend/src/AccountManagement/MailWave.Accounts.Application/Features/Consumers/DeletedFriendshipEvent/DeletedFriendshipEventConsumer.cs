using MailWave.Accounts.Application.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace MailWave.Accounts.Application.Features.Consumers.DeletedFriendshipEvent;

public class DeletedFriendshipEventConsumer: IConsumer<Mail.Contracts.Messaging.DeletedFriendshipEvent>
{
    private readonly ILogger<DeletedFriendshipEventConsumer> _logger;
    private readonly IFriendshipRepository _repository;

    public DeletedFriendshipEventConsumer(
        ILogger<DeletedFriendshipEventConsumer> logger,
        IFriendshipRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<Mail.Contracts.Messaging.DeletedFriendshipEvent> context)
    {
        var message = context.Message;

        var friendship = await _repository.GetByEmails(
            message.FirstUserEmail,
            message.SecondUserEmail,
            context.CancellationToken);

        if (friendship is null)
            throw new Exception("Friendship was not sent or friendship is not exist");

        if (!friendship.IsAccepted)
            throw new Exception("Friendship is not accepted");
        
        await _repository.Delete(friendship.Id, context.CancellationToken);

        _logger.LogInformation("Consumer: User {first} deleted {second} from friendship",
            message.FirstUserEmail, message.SecondUserEmail);
    }
}