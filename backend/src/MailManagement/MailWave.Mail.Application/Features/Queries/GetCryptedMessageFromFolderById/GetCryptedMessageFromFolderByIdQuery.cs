using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Queries.GetCryptedMessageFromFolderById;

public record GetCryptedMessageFromFolderByIdQuery(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder EmailFolder,
    uint MessageId) : IQuery;
