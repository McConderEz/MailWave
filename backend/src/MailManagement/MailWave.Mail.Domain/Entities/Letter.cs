namespace MailWave.Mail.Domain.Entities;

public class Letter
{
    public uint Id { get; set; }
    public string From { get; set; } = string.Empty;
    public List<string> To { get; set; } = [];
    public string Body { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsCrypted { get; set; }
    public bool IsSigned { get; set; }
}