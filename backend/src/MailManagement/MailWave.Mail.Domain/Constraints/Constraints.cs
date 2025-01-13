namespace MailWave.Mail.Domain.Constraints;

public class Constraints
{
    public static readonly string EMAIL_REGEX_PATTERN = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";


    public static readonly string FRIENDS_REQUEST_SUBJECT = "#@FriendRequest";
    public static readonly string CRYPTED_SUBJECT = "#@Crypted";
    public static readonly string SIGNED_SUBJECT = "#@Signed";
}