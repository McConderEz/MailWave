using FluentValidation;
using MailWave.Core.Validators;
using MailWave.Mail.Domain.Constraints;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Mail.Application.Features.Queries.GetSavedMessageById;

public class GetSavedMessageByIdValidator: AbstractValidator<GetSavedMessageByIdQuery>
{
    public GetSavedMessageByIdValidator()
    {
        RuleFor(g => g.MailCredentialsDto.Email)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("email"));
        
        RuleFor(g => g.MailCredentialsDto.Password)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("password"));

        RuleFor(g => g.MessageId)
            .GreaterThanOrEqualTo((uint)0)
            .WithError(Errors.General.ValueIsInvalid());
    }
}