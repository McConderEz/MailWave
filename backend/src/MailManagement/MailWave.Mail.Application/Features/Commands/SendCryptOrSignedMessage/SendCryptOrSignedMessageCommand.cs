using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.Mail.Application.DTOs;

namespace MailWave.Mail.Application.Features.Commands.SendCryptOrSignedMessage;

public record SendCryptOrSignedMessageCommand(
    MailCredentialsDto MailCredentialsDto,
    bool IsCrypted,
    bool IsSigned,
    string? Subject,
    string? Body,
    string Receiver,
    IEnumerable<AttachmentDto>? AttachmentDtos) : ICommand;
