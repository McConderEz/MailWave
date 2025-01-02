namespace MailWave.Accounts.Contracts.Messaging;

public record GotUserCredentialsForMailEvent(string Email, string Password);
