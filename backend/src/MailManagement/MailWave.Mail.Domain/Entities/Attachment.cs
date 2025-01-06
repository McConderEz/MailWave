namespace MailWave.Mail.Domain.Entities;

public class Attachment
{
    public Stream Content { get; set; }
    public string FileName { get; set; }
}