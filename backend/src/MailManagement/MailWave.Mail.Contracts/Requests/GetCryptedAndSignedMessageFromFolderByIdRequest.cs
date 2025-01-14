using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record GetCryptedAndSignedMessageFromFolderByIdRequest(Constraints.EmailFolder EmailFolder);
