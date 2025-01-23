using FluentValidation;
using MailWave.Core.Validators;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Accounts.Application.Features.Commands.DeleteRefreshSession;

public class DeleteRefreshTokenValidator: AbstractValidator<DeleteRefreshTokenCommand>
{
    public DeleteRefreshTokenValidator()
    {
        RuleFor(r => r.RefreshToken)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("refresh token"));
    }
}