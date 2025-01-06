using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.MoveMessage;

/// <summary>
/// Перемещение письма из одной папки в другую
/// </summary>
public class MoveMessageHandler: ICommandHandler<MoveMessageCommand>
{
    private readonly IValidator<MoveMessageCommand> _validator;
    private readonly ILogger<MoveMessageHandler> _logger;
    private readonly IMailService _mailService;

    public MoveMessageHandler(
        IValidator<MoveMessageCommand> validator,
        ILogger<MoveMessageHandler> logger,
        IMailService mailService)
    {
        _validator = validator;
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// Обработчик
    /// </summary>
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> Handle(MoveMessageCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();
        
        var messages = await _mailService.MoveMessage(
            command.MailCredentialsDto,
            command.SelectedFolder,
            command.TargetFolder,
            command.MessageId,
            cancellationToken);

        if (messages.IsFailure)
            return messages.Errors;

        _logger.LogInformation("User send message");
        
        return Result.Success();
    }
}