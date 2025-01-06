using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record MoveMessageRequest(
    Constraints.EmailFolder SelectedFolder,
    Constraints.EmailFolder TargetFolder);
