using FluentValidation;
using MailWave.Core.Validators;
using MailWave.Mail.Domain.Constraints;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Mail.Application.Features.Commands.SendScheduledMessage;

public class SendScheduledMessageValidator: AbstractValidator<SendScheduledMessageCommand>
{
    public SendScheduledMessageValidator()
    {
        RuleFor(g => g.MailCredentialsDto.Email)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("email"));
        
        RuleFor(g => g.MailCredentialsDto.Password)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("password"));

        RuleFor(g => g.EnqueueAt)
            .Must(e => e > DateTime.Now)
            .WithError(Errors.General.ValueIsInvalid());
        
        RuleForEach(r => r.Receivers)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("receivers"));

        RuleForEach(r => r.AttachmentDtos)
            .ChildRules(a =>
            {
                a.RuleFor(f => f.FileName)
                    .NotEmpty()
                    .WithError(Errors.General.ValueIsRequired("file name"));

                a.RuleFor(c => c.Content)
                    .Must(s => s.Length is > 0)
                    .WithError(Error.Null("stream.empty", "stream cannot be empty"));
            });
    }
}