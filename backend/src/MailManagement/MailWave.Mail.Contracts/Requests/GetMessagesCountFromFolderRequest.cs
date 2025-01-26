using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record GetMessagesCountFromFolderRequest(Constraints.EmailFolder SelectedFolder);
