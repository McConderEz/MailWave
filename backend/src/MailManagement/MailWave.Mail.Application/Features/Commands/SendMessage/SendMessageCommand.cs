using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.Mail.Application.DTOs;

namespace MailWave.Mail.Application.Features.Commands.SendMessage;

public record SendMessageCommand(
    MailCredentialsDto MailCredentialsDto,
    string? Subject,
    string? Body,
    IEnumerable<string> Receivers,
    IEnumerable<AttachmentDto>? AttachmentDtos) : ICommand;
