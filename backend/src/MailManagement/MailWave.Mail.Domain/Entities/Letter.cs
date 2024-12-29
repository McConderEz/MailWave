namespace MailWave.Mail.Domain.Entities;

public class Letter
{
    public Guid Id { get; set; }
    public List<string> To { get; set; } = [];
    public string Body { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public bool IsCrypted { get; set; }
    public bool IsSigned { get; set; }
}