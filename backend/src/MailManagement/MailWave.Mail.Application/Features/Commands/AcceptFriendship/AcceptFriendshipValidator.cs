﻿using FluentValidation;
using MailWave.Core.Validators;
using MailWave.Mail.Domain.Constraints;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Mail.Application.Features.Commands.AcceptFriendship;

public class AcceptFriendshipValidator: AbstractValidator<AcceptFriendshipCommand>
{
    public AcceptFriendshipValidator()
    {
        RuleFor(g => g.MailCredentialsDto.Email)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("email"));
        
        RuleFor(g => g.MailCredentialsDto.Password)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("password"));
        
        RuleFor(g => g.MessageId)
            .GreaterThanOrEqualTo((uint)1)
            .WithError(Errors.General.ValueIsInvalid());
    }
}