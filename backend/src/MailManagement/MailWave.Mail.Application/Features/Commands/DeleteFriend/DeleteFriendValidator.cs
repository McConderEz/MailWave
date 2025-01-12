using FluentValidation;
using MailWave.Core.Validators;
using MailWave.Mail.Domain.Constraints;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Mail.Application.Features.Commands.DeleteFriend;

public class DeleteFriendValidator: AbstractValidator<DeleteFriendCommand>
{
    public DeleteFriendValidator()
    {
        RuleFor(g => g.MailCredentialsDto.Email)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("email"));
        
        RuleFor(g => g.MailCredentialsDto.Password)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("password"));
        
        RuleFor(a => a.FriendEmail)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("receiver"));
    }
}