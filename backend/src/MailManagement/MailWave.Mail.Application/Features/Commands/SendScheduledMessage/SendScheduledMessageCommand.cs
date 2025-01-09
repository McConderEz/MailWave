using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.Mail.Application.DTOs;

namespace MailWave.Mail.Application.Features.Commands.SendScheduledMessage;

public record SendScheduledMessageCommand(
    MailCredentialsDto MailCredentialsDto,
    string? Subject,
    string? Body,
    DateTime EnqueueAt,
    IEnumerable<string> Receivers,
    IEnumerable<AttachmentDto>? AttachmentDtos) : ICommand;
