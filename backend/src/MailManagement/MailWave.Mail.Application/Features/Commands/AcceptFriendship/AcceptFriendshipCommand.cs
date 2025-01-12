using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Commands.AcceptFriendship;

public record AcceptFriendshipCommand(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder EmailFolder,
    uint MessageId) : ICommand;
