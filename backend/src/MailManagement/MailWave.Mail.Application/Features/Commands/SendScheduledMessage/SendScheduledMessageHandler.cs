using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.SendScheduledMessage;

/// <summary>
/// Отправка запланированного письма
/// </summary>
public class SendScheduledMessageHandler: ICommandHandler<SendScheduledMessageCommand>
{
    private readonly IValidator<SendScheduledMessageCommand> _validator;
    private readonly ILogger<SendScheduledMessageHandler> _logger;
    private readonly IMailService _mailService;

    public SendScheduledMessageHandler(
        IValidator<SendScheduledMessageCommand> validator,
        ILogger<SendScheduledMessageHandler> logger,
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
    public async Task<Result> Handle(SendScheduledMessageCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();
        
        var letter = new Letter
        {
            Body = command.Body ?? string.Empty,
            Subject = command.Subject ?? string.Empty,
            AttachmentNames = command.AttachmentDtos?.Select(a => a.FileName).ToList() ?? [],
            To = command.Receivers.ToList()
        };

        var attachments = command.AttachmentDtos?.Select(a => new Attachment
        {
            Content = a.Content,
            FileName = a.FileName
        });
        
        var result = await _mailService.SendScheduledMessage(
            command.MailCredentialsDto,
            attachments,
            letter,
            command.EnqueueAt,
            cancellationToken);

        if (result.IsFailure)
            return result.Errors;

        _logger.LogInformation("User sent message");
        
        return Result.Success();
    }
}