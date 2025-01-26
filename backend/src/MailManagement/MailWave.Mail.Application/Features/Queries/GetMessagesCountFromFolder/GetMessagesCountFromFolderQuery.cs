using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Queries.GetMessagesCountFromFolder;

public record GetMessagesCountFromFolderQuery(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder EmailFolder) : IQuery;