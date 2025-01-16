using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Commands.VerifyMessage;

public record VerifyMessageCommand(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder EmailFolder,
    uint MessageId) : ICommand;
