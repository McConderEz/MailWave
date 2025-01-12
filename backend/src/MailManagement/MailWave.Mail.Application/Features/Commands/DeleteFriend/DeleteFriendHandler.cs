using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Contracts.Messaging;
using MailWave.SharedKernel.Shared;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.DeleteFriend;

/// <summary>
/// Удалить из друзей
/// </summary>
public class DeleteFriendHandler: ICommandHandler<DeleteFriendCommand>
{
    private readonly ILogger<DeleteFriendHandler> _logger;
    private readonly IValidator<DeleteFriendCommand> _validator;
    private readonly IPublishEndpoint _publishEndpoint;

    public DeleteFriendHandler(
        ILogger<DeleteFriendHandler> logger,
        IValidator<DeleteFriendCommand> validator,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _validator = validator;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Обработчик
    /// </summary>
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> Handle(DeleteFriendCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();
        
        await _publishEndpoint.Publish(
            new DeletedFriendshipEvent(
                command.MailCredentialsDto.Email,
                command.FriendEmail), 
            cancellationToken);
        
        _logger.LogInformation("User {first} deleted {second} from friends",
            command.MailCredentialsDto.Email, command.FriendEmail);

        return Result.Success();
    }
}