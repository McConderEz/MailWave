using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Commands.MoveMessage;

public record MoveMessageCommand(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder SelectedFolder,
    Constraints.EmailFolder TargetFolder,
    uint MessageId) : ICommand;
