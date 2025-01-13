using FluentValidation;
using MailWave.Core.Validators;
using MailWave.Mail.Domain.Constraints;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Mail.Application.Features.Commands.SendCryptOrSignedMessage;

public class SendCryptOrSignedMessageValidator : AbstractValidator<SendCryptOrSignedMessageCommand>
{
    public SendCryptOrSignedMessageValidator()
    {
        RuleFor(g => g.MailCredentialsDto.Email)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("email"));
        
        RuleFor(g => g.MailCredentialsDto.Password)
            .NotEmpty()
            .WithError(Errors.General.ValueIsRequired("password"));
        
        RuleFor(a => a.Receiver)
            .Matches(Constraints.EMAIL_REGEX_PATTERN)
            .WithError(Errors.General.ValueIsInvalid("receiver"));

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