using FluentValidation;
using MailWave.Accounts.Contracts;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.CryptProviders;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.SendCryptOrSignedMessage;

public class SendCryptOrSignedMessageHandler : ICommandHandler<SendCryptOrSignedMessageCommand>
{
    private readonly ILogger<SendCryptOrSignedMessageHandler> _logger;
    private readonly IValidator<SendCryptOrSignedMessageCommand> _validator;
    private readonly IMailService _mailService;
    private readonly IDesCryptProvider _desCryptProvider;
    private readonly IAccountContract _accountContract;

    public SendCryptOrSignedMessageHandler(
        ILogger<SendCryptOrSignedMessageHandler> logger,
        IValidator<SendCryptOrSignedMessageCommand> validator,
        IMailService mailService,
        IAccountContract accountContract,
        IDesCryptProvider desCryptProvider)
    {
        _logger = logger;
        _validator = validator;
        _mailService = mailService;
        _accountContract = accountContract;
        _desCryptProvider = desCryptProvider;
    }

    public async Task<Result> Handle(
        SendCryptOrSignedMessageCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var (publicKey, privateKey) = await _accountContract.GetCryptData(
            command.MailCredentialsDto.Email,
            command.Receiver,
            cancellationToken);
        
        if (publicKey == String.Empty || privateKey == String.Empty)
            return Errors.MailErrors.NotFriendError();

        var letter = new Letter() { Subject = command.Subject};

        if (command.IsCrypted)
        {
            if (command.Body is not null && command.Body != String.Empty)
            {
                var result = CryptBody(command, letter);
                if (result.IsFailure)
                    return result.Errors;
            }

            if (command.AttachmentDtos?.Count() > 0)
            {
                var result = CryptAttachments(command, letter);
                if (result.IsFailure)
                    return result.Errors;
            }
        }

        /*var letter = new Letter
        {
            Body = command.Body ?? string.Empty,
            Subject = command.Subject ?? string.Empty,
            AttachmentNames = command.AttachmentDtos?.Select(a => a.FileName).ToList() ?? [],
            To = [command.Receiver]
        };*/

        var attachments = command.AttachmentDtos?.Select(a => new Attachment
        {
            Content = a.Content,
            FileName = a.FileName
        });

        return Result.Success();
    }

    private Result CryptAttachments(SendCryptOrSignedMessageCommand command, Letter letter)
    {
        throw new NotImplementedException();
    }

    private Result CryptBody(SendCryptOrSignedMessageCommand command, Letter letter)
    {
        letter.IsCrypted = true;
        letter.Subject += Domain.Constraints.Constraints.CRYPTED_SUBJECT;

        var keys = _desCryptProvider.GenerateKey();
        if (keys.IsFailure)
            return Error.Failure("generation.keys.error", "Generation keys failure");

        if (command.Body is null)
            return Error.Null("body.null", "Body is null");
            
        var body = _desCryptProvider.Encrypt(command.Body, keys.Value.key, keys.Value.iv);
        if (body.IsFailure)
            return body.Errors;

        letter.Body = body.Value;
        
        return Result.Success();
    }
}