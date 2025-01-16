namespace MailWave.Core.DTOs;

public record LetterDto(
    uint MessageId,
    string? Subject,
    string? Body,
    bool IsCrypted,
    bool IsSigned,
    string From);
