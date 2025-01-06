namespace MailWave.Mail.Contracts.Requests;

public record SendMessageRequest(
    string? Subject,
    string? Body,
    IEnumerable<string> Receivers);
