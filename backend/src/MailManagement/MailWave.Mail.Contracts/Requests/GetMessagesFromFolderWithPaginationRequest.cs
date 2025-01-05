

using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record GetMessagesFromFolderWithPaginationRequest(
    Constraints.EmailFolder EmailFolder,
    int Page,
    int PageSize);
