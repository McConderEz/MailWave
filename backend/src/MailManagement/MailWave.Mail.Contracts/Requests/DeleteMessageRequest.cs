using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record DeleteMessageRequest(Constraints.EmailFolder SelectedFolder);
