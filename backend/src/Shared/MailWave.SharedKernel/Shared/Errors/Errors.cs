namespace MailWave.SharedKernel.Shared.Errors;

public static class Errors
{
    public static class General
    {
        public static Error ValueIsInvalid(string? name = null)
        {
            var label = name ?? "value";
            return Error.Validation("Invalid.input", $"{label} is invalid");
        }

        public static Error NotFound(Guid? id = null)
        {
            var forId = id == null ? "" : $"for Id:{id}";
            return Error.NotFound("Record.not.found", $"record not found {forId}");
        }

        public static Error Null(string? name = null)
        {
            var label = name ?? "value";
            return Error.Null("Null.entity", $"{label} is null");
        }

        public static Error ValueIsRequired(string? name = null)
        {
            var label = name == null ? "" : " " + name + " ";
            return Error.Validation("Invalid.length",$"invalid{label}length");
        }
        public static Error AlreadyExist()
        {
            return Error.Validation("Record.already.exist", $"Records already exist");
        }
    }

    public static class MailErrors
    {
        public static Error ConnectionError()
        {
            return Error.Failure(
                "connection.error", "Cannot connect to mail, probably incorrect credentials");
        }
        
        public static Error IncorrectSubjectFormat()
        {
            return Error.Failure(
                "subject.format.error", "Subject has incorrect format");
        }
        
        public static Error NotFriendError()
        {
            return Error.Conflict("not.friend.error", $"This user is not your friend");
        }
    }

    public static class Tokens
    {
        public static Error ExpiredToken()
        {
            return Error.Validation("token.is.expired", $"Your token is expired");
        }
        
        public static Error InvalidToken()
        {
            return Error.Validation("token.is.invalid", $"Your token is invalid");
        }
        
    }
    

    public static class User
    {
        public static Error InvalidCredentials()
        {
            return Error.Validation("credentials.is.invalid", "Your credentials is invalid");
        }
    }
}