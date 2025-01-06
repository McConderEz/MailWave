using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Commands.DeleteMessage;

public record DeleteMessageCommand(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder SelectedFolder,
    uint MessageId) : ICommand;
