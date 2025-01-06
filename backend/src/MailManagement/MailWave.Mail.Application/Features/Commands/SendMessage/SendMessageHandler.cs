using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.SendMessage;

/// <summary>
/// Отправка письма с вложением или без
/// </summary>
public class SendMessageHandler: ICommandHandler<SendMessageCommand>
{
    private readonly IValidator<SendMessageCommand> _validator;
    private readonly ILogger<SendMessageHandler> _logger;
    private readonly IMailService _mailService;

    public SendMessageHandler(
        IValidator<SendMessageCommand> validator,
        ILogger<SendMessageHandler> logger,
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
    public async Task<Result> Handle(SendMessageCommand command, CancellationToken cancellationToken = default)
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
        
        var messages = await _mailService.SendMessage(
            command.MailCredentialsDto,
            attachments,
            letter,
            cancellationToken);

        if (messages.IsFailure)
            return messages.Errors;

        _logger.LogInformation("User sent message");
        
        return Result.Success();
    }
}