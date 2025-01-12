using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Contracts.Messaging;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.AcceptFriendship;

/// <summary>
/// Принять запрос в друзья
/// </summary>
public class AcceptFriendshipHandler: ICommandHandler<AcceptFriendshipCommand>
{
    private readonly ILogger<AcceptFriendshipHandler> _logger;
    private readonly IMailService _mailService;
    private readonly IValidator<AcceptFriendshipCommand> _validator;
    private readonly IPublishEndpoint _publishEndpoint;

    public AcceptFriendshipHandler(
        ILogger<AcceptFriendshipHandler> logger,
        IMailService mailService,
        IValidator<AcceptFriendshipCommand> validator,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _mailService = mailService;
        _validator = validator;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Обработчик
    /// </summary>
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> Handle(AcceptFriendshipCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var message = await _mailService.GetMessage(
            command.MailCredentialsDto,
            command.EmailFolder,
            command.MessageId,
            cancellationToken);

        if (message.IsFailure)
            return message.Errors;

        if (message.Value.Subject is null ||
            !message.Value.Subject.Contains(Domain.Constraints.Constraints.FRIENDS_REQUEST_SUBJECT))
            return Errors.MailErrors.IncorrectSubjectFormat();

        await _publishEndpoint.Publish(new AcceptedFriendshipEvent(command.MailCredentialsDto.Email, message.Value.From),
            cancellationToken);
        
        _logger.LogInformation("User {first} accepted friendship with {second}",
            command.MailCredentialsDto.Email, message.Value.From);
        
        return Result.Success();
    }
}