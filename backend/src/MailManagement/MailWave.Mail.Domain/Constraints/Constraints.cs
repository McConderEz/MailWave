namespace MailWave.Mail.Domain.Constraints;

public class Constraints
{
    public static readonly string EMAIL_REGEX_PATTERN = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
    
    public enum EmailFolder
    {
        Inbox = 0,
        Sent = 1,
        Drafts = 2,
        Junk = 3,
        Trash = 4,
    }
}