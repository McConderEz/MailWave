using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record SaveFilesRequest(
    string DirectoryPath,
    Constraints.EmailFolder SelectedFolder,
    uint MessageId,
    string FileName);
