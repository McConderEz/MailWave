using FluentValidation;
using MailWave.Core.Validators;
using MailWave.SharedKernel.Shared.Errors;
using Constraints = MailWave.Mail.Domain.Constraints.Constraints;

namespace MailWave.Mail.Application.Features.Commands.SendMessage;

public class SendMessageValidator: AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(g => g.MailCredentialsDto.Email)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("email"));
        
        RuleFor(g => g.MailCredentialsDto.Password)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("password"));
        
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