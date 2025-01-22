using FluentValidation;
using MailWave.Core.Validators;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Accounts.Application.Features.Commands.Refresh;

public class RefreshTokenCommandValidator: AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(r => r.RefreshToken)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("refresh token"));
    }
}