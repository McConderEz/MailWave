using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.CryptProviders;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Contracts.Messaging;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.AddFriend;

/// <summary>
/// Добавление в друзья
/// </summary>
public class AddFriendHandler: ICommandHandler<AddFriendCommand>
{
    private readonly ILogger<AddFriendHandler> _logger;
    private readonly IValidator<AddFriendCommand> _validator;
    private readonly IMailService _mailService;
    private readonly IRsaCryptProvider _rsaCryptProvider;
    private readonly IPublishEndpoint _publishEndpoint;

    public AddFriendHandler(
        ILogger<AddFriendHandler> logger,
        IValidator<AddFriendCommand> validator,
        IMailService mailService,
        IRsaCryptProvider rsaCryptProvider,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _validator = validator;
        _mailService = mailService;
        _rsaCryptProvider = rsaCryptProvider;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Обработчик
    /// </summary>
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> Handle(AddFriendCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var keys = _rsaCryptProvider.GenerateKey();

        var letter = new Letter
        {
            From = command.MailCredentialsDto.Email,
            To = [command.Receiver],
            Subject = Domain.Constraints.Constraints.FRIENDS_REQUEST_SUBJECT,
            Body = keys.publicKey + "#" + keys.privateKey
        };
        
        var result = await _mailService.SendMessage(
            command.MailCredentialsDto, null, letter, cancellationToken);

        if (result.IsFailure)
            return result.Errors;
        
        await _publishEndpoint.Publish(new GotFriendshipDataEvent(
                command.MailCredentialsDto.Email,
                command.Receiver,
                Convert.ToBase64String(keys.publicKey),
                Convert.ToBase64String(keys.privateKey)),
            cancellationToken);
        
        _logger.LogInformation("Sent friend request from {first} to {second}",
            command.MailCredentialsDto.Email,
            command.Receiver);
        
        return Result.Success();
    }
}