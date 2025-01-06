

using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record GetMessageFromFolderByIdRequest(Constraints.EmailFolder EmailFolder);
