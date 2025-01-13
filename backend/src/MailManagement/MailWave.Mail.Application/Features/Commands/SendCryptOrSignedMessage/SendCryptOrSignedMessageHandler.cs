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
    private readonly IRsaCryptProvider _rsaCryptProvider;
    private readonly IAccountContract _accountContract;

    public SendCryptOrSignedMessageHandler(
        ILogger<SendCryptOrSignedMessageHandler> logger,
        IValidator<SendCryptOrSignedMessageCommand> validator,
        IMailService mailService,
        IAccountContract accountContract,
        IDesCryptProvider desCryptProvider,
        IRsaCryptProvider rsaCryptProvider)
    {
        _logger = logger;
        _validator = validator;
        _mailService = mailService;
        _accountContract = accountContract;
        _desCryptProvider = desCryptProvider;
        _rsaCryptProvider = rsaCryptProvider;
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

        var letter = new Letter() { Subject = command.Subject, To = [command.Receiver] };
        List<Attachment> attachments = [];
        
        if (command.IsCrypted)
        {
            var keys = _desCryptProvider.GenerateKey();
            if (keys.IsFailure)
                return Error.Failure("generation.keys.error", "Generation keys failure");
            
            if (command.Body is not null && command.Body != String.Empty)
            {
                var result = CryptBody(command, letter, keys.Value.key, keys.Value.iv);
                if (result.IsFailure)
                    return result.Errors;
            }

            if (command.AttachmentDtos?.Count() > 0)
            {
                var result = await CryptAttachments(command, letter, keys.Value.key, keys.Value.iv);
                if (result.IsFailure)
                    return result.Errors;
                attachments = result.Value;
            }
            
            attachments.AddRange(await AttachEncryptedKeyAndIv(keys.Value.key, keys.Value.iv, publicKey));
        }

        var sendingResult = await _mailService.SendMessage(
            command.MailCredentialsDto,
            attachments,
            letter,
            cancellationToken);

        return sendingResult.IsFailure ? sendingResult.Errors : Result.Success();
    }

    /// <summary>
    /// Шифрование ключа и вектора инициализации RSA алгоритмом
    /// </summary>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <param name="publicKey">Публичный ключ RSA</param>
    /// <returns></returns>
    private async Task<List<Attachment>> AttachEncryptedKeyAndIv(string key, string iv, string publicKey)
    {
        var encryptedKey = _rsaCryptProvider.Encrypt(key, publicKey);
        var encryptedIv = _rsaCryptProvider.Encrypt(iv, publicKey);

        var attachments = new List<Attachment>
        {
            new Attachment
            {
                FileName = "key.key",
                Content = new MemoryStream(Convert.FromBase64String(encryptedKey.Value))
            },
            new Attachment
            {
                FileName = "iv.iv",
                Content = new MemoryStream(Convert.FromBase64String(encryptedIv.Value))
            }
        };

        return attachments;
    }

    /// <summary>
    /// Шифрование вложений письма
    /// </summary>
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="letter">Письмо</param>
    /// <param name="key">Ключ Des</param>
    /// <param name="iv">Вектор инициализации Des</param>
    /// <returns></returns>
    private async Task<Result<List<Attachment>>> CryptAttachments(
        SendCryptOrSignedMessageCommand command,
        Letter letter,
        string key, string iv)
    {
        var attachments = new List<Attachment>();

        using var memoryStream = new MemoryStream();
        
        foreach (var attachment in command.AttachmentDtos!)
        {
            await memoryStream.CopyToAsync(attachment.Content);
            
            var data = memoryStream.ToArray();

            var stringData = Convert.ToBase64String(data);

            var encryptData = _desCryptProvider.Encrypt(stringData, key, iv);
            if (encryptData.IsFailure)
                return encryptData.Errors;

            attachments.Add(new Attachment
            {
                FileName = attachment.FileName,
                Content = new MemoryStream(Convert.FromBase64String(encryptData.Value))
            });
            
            letter.AttachmentNames.Add(attachment.FileName);
        }
        
        return attachments;
    }

    /// <summary>
    /// Шифрование основного содержимого письма
    /// </summary>
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="letter">Письмо</param>
    /// <param name="key">Ключ Des</param>
    /// <param name="iv">Вектор инициализации Des</param>
    /// <returns></returns>
    private Result CryptBody(SendCryptOrSignedMessageCommand command, Letter letter, string key, string iv)
    {
        letter.IsCrypted = true;
        letter.Subject += Domain.Constraints.Constraints.CRYPTED_SUBJECT;
        
        if (command.Body is null)
            return Error.Null("body.null", "Body is null");
            
        var body = _desCryptProvider.Encrypt(command.Body, key, iv);
        if (body.IsFailure)
            return body.Errors;

        letter.Body = body.Value;
        
        return Result.Success();
    }
}