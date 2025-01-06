using FluentValidation;
using MailWave.Core.Validators;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Mail.Application.Features.Commands.MoveMessage;

public class MoveMessageValidator: AbstractValidator<MoveMessageCommand>
{
    public MoveMessageValidator()
    {
        RuleFor(g => g.MessageId)
            .GreaterThanOrEqualTo((uint)1)
            .WithError(Errors.General.ValueIsInvalid());
    }
}