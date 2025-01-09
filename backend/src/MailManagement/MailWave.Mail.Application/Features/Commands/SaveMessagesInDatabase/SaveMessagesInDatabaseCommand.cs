
using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Commands.SaveMessagesInDatabase;

public record SaveMessagesInDatabaseCommand(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder EmailFolder,
    IEnumerable<uint> MessageIds) : ICommand;
