using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.DeleteMessage;

/// <summary>
/// Удаление письма из почты
/// </summary>
public class DeleteMessageHandler : ICommandHandler<DeleteMessageCommand>
{
    private readonly IValidator<DeleteMessageCommand> _validator;
    private readonly ILogger<DeleteMessageHandler> _logger;
    private readonly IMailService _mailService;

    public DeleteMessageHandler(
        IValidator<DeleteMessageCommand> validator,
        ILogger<DeleteMessageHandler> logger,
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
    public async Task<Result> Handle(DeleteMessageCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();
        
        var messages = await _mailService.DeleteMessage(
            command.MailCredentialsDto,
            command.SelectedFolder,
            command.MessageId,
            cancellationToken);

        if (messages.IsFailure)
            return messages.Errors;

        _logger.LogInformation("User deleted message");
        
        return Result.Success();
    }
}