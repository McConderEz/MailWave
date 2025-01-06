using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;


namespace MailWave.Mail.Application.Features.Queries.GetMessageFromFolderById;

public record GetMessageFromFolderByIdQuery(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder EmailFolder,
    uint MessageId) : IQuery;
