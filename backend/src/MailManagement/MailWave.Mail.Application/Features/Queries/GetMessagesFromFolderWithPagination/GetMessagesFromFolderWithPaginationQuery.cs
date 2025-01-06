using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Queries.GetMessagesFromFolderWithPagination;

public record GetMessagesFromFolderWithPaginationQuery(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder EmailFolder,
    int Page,
    int PageSize) : IQuery;
