using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace MailWave.Mail.Domain.Shared;

public partial class EmailValidator
{
    private const string EMAIL_REGEX_PATTERN = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
    private const string INVALID_EMAIL_ERR = "Request doesn't contain any valid reciever's adress. Aborting sending.";
    

    /// <summary>
    /// Метод, вызывающий валидацию email-адрессов 
    /// </summary>
    /// <param name="addresses">список адрессов</param>
    /// <returns></returns>
    public Result<List<string>> Execute(List<string> addresses)
    {
        for (int i = addresses.Count - 1; i >= 0; i--)
        {
            if (EmailRegex().IsMatch(addresses[i]) == false)
            {
                addresses.RemoveAt(i);
            }
        }

        if (addresses.Count == 0)
        {
            return Result.Failure<List<string>>(INVALID_EMAIL_ERR);
        }

        return addresses;
    }

    [GeneratedRegex(EMAIL_REGEX_PATTERN)]
    private static partial Regex EmailRegex();
}