namespace MailWave.Mail.Contracts.Requests;

public record SendCryptedAndSignedMessageRequest(
    string? Subject,
    string? Body,
    bool IsCrypted,
    bool IsSigned,
    string Receivers);
