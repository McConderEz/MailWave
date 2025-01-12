namespace MailWave.Mail.Domain.Constraints;

public class Constraints
{
    public static readonly string EMAIL_REGEX_PATTERN = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";


    public static readonly string FRIENDS_REQUEST_SUBJECT = "#@FriendRequest";
}