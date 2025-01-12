namespace MailWave.Mail.Contracts.Messaging;

public record GotFriendshipDataEvent(
    string FirstEmail,
    string SecondEmail,
    string PublicKey,
    string PrivateKey);
