using FluentValidation;
using MailWave.Core.Validators;
using MailWave.Mail.Domain.Constraints;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Mail.Application.Features.Commands.SaveMessagesInDatabase;

public class SaveMessagesInDatabaseValidator: AbstractValidator<SaveMessagesInDatabaseCommand>
{
    public SaveMessagesInDatabaseValidator()
    {
        RuleFor(g => g.MailCredentialsDto.Email)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("email"));
        
        RuleFor(g => g.MailCredentialsDto.Password)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("password"));
        
        RuleForEach(r => r.MessageIds)
            .ChildRules(a =>
            {
                a.RuleFor(m => m)
                    .GreaterThanOrEqualTo((uint)1)
                    .WithError(Errors.General.ValueIsInvalid("message id"));
            });
    }
}