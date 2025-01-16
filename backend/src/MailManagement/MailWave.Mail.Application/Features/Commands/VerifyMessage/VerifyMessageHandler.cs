using System.Security;
using System.Text;
using FluentValidation;
using MailWave.Accounts.Contracts;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.CryptProviders;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Contracts;
using MailWave.Mail.Contracts.Responses;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.VerifyMessage;

public class VerifyMessageHandler: ICommandHandler<VerifyMessageCommand, VerifyResponse>
{
    private readonly IMailContract _mailContract;
    private readonly IValidator<VerifyMessageCommand> _validator;
    private readonly ILogger<VerifyMessageHandler> _logger;
    private readonly IDesCryptProvider _desCryptProvider;
    private readonly IRsaCryptProvider _rsaCryptProvider;
    private readonly IMd5CryptProvider _md5CryptProvider;
    private readonly IAccountContract _accountContract;
    private readonly IMailService _mailService;

    public VerifyMessageHandler(
        IMailContract mailContract,
        IValidator<VerifyMessageCommand> validator,
        ILogger<VerifyMessageHandler> logger,
        IDesCryptProvider desCryptProvider,
        IRsaCryptProvider rsaCryptProvider,
        IAccountContract accountContract,
        IMailService mailService,
        IMd5CryptProvider md5CryptProvider)
    {
        _mailContract = mailContract;
        _validator = validator;
        _logger = logger;
        _desCryptProvider = desCryptProvider;
        _rsaCryptProvider = rsaCryptProvider;
        _accountContract = accountContract;
        _mailService = mailService;
        _md5CryptProvider = md5CryptProvider;
    }

    public async Task<Result<VerifyResponse>> Handle(
        VerifyMessageCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var decryptedBody = await _mailContract.GetDecryptedBody(
            command.MailCredentialsDto, command.EmailFolder, command.MessageId, cancellationToken);

        if (decryptedBody.IsFailure)
            return decryptedBody.Errors;

        var commonHash = new StringBuilder();
        
        var bodyHash = _md5CryptProvider.ComputeHash(decryptedBody.Value);
        if (bodyHash.IsFailure)
            return bodyHash.Errors;

        commonHash.Append(bodyHash.Value);
        
        var message = await _mailService.GetMessage(
            command.MailCredentialsDto,
            command.EmailFolder,
            command.MessageId,
            cancellationToken);
        
        var (publicKey, privateKey) = await _accountContract.GetCryptData(
            command.MailCredentialsDto.Email,
            message.Value.From,
            cancellationToken);
        
        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
            return Errors.MailErrors.NotFriendError();
        
        var attachments = await _mailService.GetAttachmentsOfMessage(
            command.MailCredentialsDto,
            command.EmailFolder,
            command.MessageId,
            cancellationToken);
        
        if (attachments.IsFailure)
            return attachments.Errors;
        
        var desData = await GetDesData(attachments.Value, privateKey, cancellationToken);
        if (desData.IsFailure)
            return desData.Errors;
        
        if (message.Value is { IsCrypted: true })
        { 
            await GetHashCryptedAttachments(
                commonHash,
                attachments.Value,
                desData.Value.key,
                desData.Value.iv,
                cancellationToken);
        }
        else
        {
            await GetHashAttachments(commonHash, attachments.Value, cancellationToken);
        }

        //TODO: Работаёт херово, отладить вычисление хэшей
        var result = await Verify(attachments, commonHash, publicKey, cancellationToken);

        return new VerifyResponse(result.Value.ToString());
    }

    /// <summary>
    /// Проверка ЭЦП общего хэша
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="commonHash">Общий хэш</param>
    /// <param name="publicKey">Публичный ключ RSA</param>
    /// <returns></returns>
    private async Task<Result<bool>> Verify(
        Result<List<Attachment>> attachments,
        StringBuilder commonHash,
        string publicKey,
        CancellationToken cancellationToken = default)
    {
        var sign = attachments.Value.FirstOrDefault(a => a.FileName.EndsWith(".sign"));

        if (sign is null)
            return Error.NotFound("sign.not.found", "sign not found");

        using var memoryStream = new MemoryStream();

        await sign.Content.CopyToAsync(memoryStream, cancellationToken);

        var result = _rsaCryptProvider.Verify(
            commonHash.ToString(),
            memoryStream.ToArray(),
            Convert.FromBase64String(publicKey));

        if (result.IsFailure)
            return result.Errors;
        
        return result;
    }

    /// <summary>
    /// Получение хэша вложений
    /// </summary>
    /// <param name="commonHash">Общий хэш</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    private async Task GetHashAttachments(
        StringBuilder commonHash,
        List<Attachment> attachments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var attachment in attachments)
            {
                if (attachment.FileName.EndsWith(".sign"))
                    continue;
                
                using var memoryStream = new MemoryStream();
                
                await attachment.Content.CopyToAsync(memoryStream, cancellationToken);

                var data = memoryStream.ToArray();
                
                commonHash.Append(_md5CryptProvider.ComputeHash(Encoding.UTF8.GetString(data)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Fail to save files. Ex. msg: {ex}", ex.Message);
        }
    }

    /// <summary>
    /// Расшифровка и получение хэша вложений
    /// </summary>
    /// <param name="commonHash">Общий хэш</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    private async Task GetHashCryptedAttachments(
        StringBuilder commonHash,
        List<Attachment> attachments,
        byte[] key,
        byte[] iv,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var attachment in attachments)
            {
                using var memoryStream = new MemoryStream();

                if (attachment.FileName.EndsWith(".key") || 
                    attachment.FileName.EndsWith(".iv")  || 
                    attachment.FileName.EndsWith(".sign"))
                    continue;

                await attachment.Content.CopyToAsync(memoryStream, cancellationToken);

                var data = memoryStream.ToArray();

                var decryptedData = _desCryptProvider.Decrypt(data, key, iv);

                if (decryptedData.IsFailure) return;

                var result = _md5CryptProvider.ComputeHash(Encoding.UTF8.GetString(decryptedData.Value));
                if (result.IsFailure)
                    return;
                
                commonHash.Append(result.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Fail to decrypt and save files. Ex. msg: {ex}", ex.Message);
        }
    }
    
    /// <summary>
    /// Получение ключа и вектора инициализации DES
    /// </summary>
    /// <param name="attachments">Вложения</param>
    /// <param name="privateKey">Приватный ключ RSA</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    private async Task<Result<(byte[] key, byte[] iv)>> GetDesData(
        List<Attachment> attachments,
        string privateKey,
        CancellationToken cancellationToken = default)
    {
        var key = attachments.FirstOrDefault(a => a.FileName.EndsWith(".key"));
        if (key is null)
            return Error.Null("key.null", "Key is null");
        
        var iv = attachments.FirstOrDefault(a => a.FileName.EndsWith(".iv"));
        if (iv is null)
            return Error.Null("iv.null", "IV is null");
        
        using var srKey = new StreamReader(key.Content, Encoding.UTF8);
        using var srIv = new StreamReader(iv.Content, Encoding.UTF8);
        
        var keyString = await srKey.ReadToEndAsync(cancellationToken);
        var ivString = await srIv.ReadToEndAsync(cancellationToken);

        var decryptedKey = _rsaCryptProvider.Decrypt(
            keyString, Convert.FromBase64String(privateKey));
        
        var decryptedIv = _rsaCryptProvider.Decrypt(
            ivString, Convert.FromBase64String(privateKey));
        
        return (decryptedKey.Value, decryptedIv.Value);
    }
}