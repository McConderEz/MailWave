namespace MailWave.Mail.Infrastructure.Model;

public class ClientSession
{
    public required string Email { get; set; }
    public DateTime LastSmtpActivity { get; set; }
    public bool IsSmtpActive { get; set; }
    public DateTime LastImapActivity { get; set; }
    public bool IsImapActive { get; set; }
}