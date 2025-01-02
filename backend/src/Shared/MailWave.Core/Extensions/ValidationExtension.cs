using FluentValidation.Results;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Core.Extensions;

public static class ValidationExtension
{
    public static ErrorList ToErrorList(this ValidationResult validationResult)
    {
        var validationErrors = validationResult.Errors;

        //TODO: Ошибка десериализации, пофиксить позже
        var errors = from validationError in validationErrors
            let errorMessage = validationError.ErrorMessage
            let error = Error.Deserialize(errorMessage)
            select Error.Validation(error.ErrorCode, error.ErrorMessage, validationError.PropertyName);

        return new ErrorList(errors);
    }
}