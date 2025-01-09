namespace MailWave.Mail.Contracts.Requests;

public record SendScheduledMessageRequest(
    string? Subject,
    string? Body,
    DateTime EnqueueAt,
    IEnumerable<string> Receivers);
