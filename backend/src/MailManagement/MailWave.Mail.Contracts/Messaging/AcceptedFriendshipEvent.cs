namespace MailWave.Mail.Contracts.Messaging;

public record AcceptedFriendshipEvent(string FirstUserEmail, string SecondUserEmail);
