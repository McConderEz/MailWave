namespace MailWave.Mail.Contracts.Messaging;

public record DeletedFriendshipEvent(string FirstUserEmail, string SecondUserEmail);
