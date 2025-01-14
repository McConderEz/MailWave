using System.Text;
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

namespace MailWave.Mail.Application.Features.Queries.GetCryptedMessageFromFolderById;

public class GetCryptedMessageFromFolderByIdHandler: IQueryHandler<Letter, GetCryptedMessageFromFolderByIdQuery>
{
    private readonly IValidator<GetCryptedMessageFromFolderByIdQuery> _validator;
    private readonly ILogger<GetCryptedMessageFromFolderByIdHandler> _logger;
    private readonly IDesCryptProvider _desCryptProvider;
    private readonly IRsaCryptProvider _rsaCryptProvider;
    private readonly IAccountContract _accountContract;
    private readonly IMailService _mailService;

    public GetCryptedMessageFromFolderByIdHandler(
        IValidator<GetCryptedMessageFromFolderByIdQuery> validator,
        ILogger<GetCryptedMessageFromFolderByIdHandler> logger,
        IDesCryptProvider desCryptProvider,
        IRsaCryptProvider rsaCryptProvider,
        IAccountContract accountContract,
        IMailService mailService)
    {
        _validator = validator;
        _logger = logger;
        _desCryptProvider = desCryptProvider;
        _rsaCryptProvider = rsaCryptProvider;
        _accountContract = accountContract;
        _mailService = mailService;
    }

    public async Task<Result<Letter>> Handle(
        GetCryptedMessageFromFolderByIdQuery query, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var message = await _mailService.GetMessage(
            query.MailCredentialsDto,
            query.EmailFolder,
            query.MessageId,
            cancellationToken);

        if (message.Value is { IsCrypted: false, IsSigned: false })
            return Error.Failure("message.not.crypted.signed", "Message is not crypted/signed");
        
        var (publicKey, privateKey) = await _accountContract.GetCryptData(
            query.MailCredentialsDto.Email,
            message.Value.From,
            cancellationToken);
        
        if (publicKey == String.Empty || privateKey == String.Empty)
            return Errors.MailErrors.NotFriendError();
        
        if (message.IsFailure)
            return message.Errors;

        var attachments = await _mailService.GetAttachmentsOfMessage(
            query.MailCredentialsDto,
            query.EmailFolder,
            query.MessageId,
            cancellationToken);

        if (attachments.IsFailure)
            return attachments.Errors;

        var key = attachments.Value.FirstOrDefault(a => a.FileName.EndsWith(".key"));
        using var srKey = new StreamReader(key.Content, Encoding.UTF8);
        var keyString = await srKey.ReadToEndAsync(cancellationToken);
        
        var iv = attachments.Value.FirstOrDefault(a => a.FileName.EndsWith(".iv"));
        using var srIv = new StreamReader(iv!.Content, Encoding.UTF8);
        var ivString = await srIv.ReadToEndAsync(cancellationToken);
        
        var decryptedKey = _rsaCryptProvider.Decrypt(keyString, Convert.FromBase64String(privateKey));
        var decryptedIv = _rsaCryptProvider.Decrypt(ivString, Convert.FromBase64String(privateKey));

        var decryptedBody = _desCryptProvider.Decrypt(message.Value.Body!, decryptedKey.Value, decryptedIv.Value);

        if (decryptedBody.IsFailure)
            return decryptedBody.Errors;

        var body = Convert.ToBase64String(decryptedBody.Value);
        
        attachments.Value.ForEach(a => a.Content.Close());
        
        _logger.LogInformation("User {email} got message from folder {folder}",
            query.MailCredentialsDto.Email, query.EmailFolder);
        
        return message;
    }
}