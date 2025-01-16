using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record VerifyMessageRequest(Constraints.EmailFolder EmailFolder, uint MessageId);
