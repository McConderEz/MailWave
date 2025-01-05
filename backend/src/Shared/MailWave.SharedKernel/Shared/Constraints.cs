using System.Text.RegularExpressions;

namespace MailWave.SharedKernel.Shared;

public static partial class Constraints
{
    public static readonly int MAX_VALUE_LENGTH = 100;
    public static readonly double MIN_VALUE = 0;
    public static readonly int MIN_LENGTH_PASSWORD = 8;
    
    public enum EmailFolder
    {
        Inbox = 0,
        Sent = 1,
        Drafts = 2,
        Junk = 3,
        Trash = 4,
    }
    
    public static readonly Regex ValidationRegex = new Regex(
        @"^[\w-\.]{1,40}@([\w-]+\.)+[\w-]{2,4}$",
        RegexOptions.Singleline | RegexOptions.Compiled);
    
}