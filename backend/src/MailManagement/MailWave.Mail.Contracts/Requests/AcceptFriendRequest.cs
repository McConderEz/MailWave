using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record AcceptFriendRequest(Constraints.EmailFolder EmailFolder, uint MessageId);
