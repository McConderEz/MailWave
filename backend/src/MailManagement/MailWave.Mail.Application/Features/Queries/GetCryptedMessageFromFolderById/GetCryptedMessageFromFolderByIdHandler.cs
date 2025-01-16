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

/// <summary>
/// Получение зашифрованного сообщения
/// </summary>
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

    /// <summary>
    /// Обработчик
    /// </summary>
    /// <param name="query">Запрос с входными параметрами</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
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

        if (message.Value is { IsCrypted: false, IsSigned: false})
            return Error.Failure("message.not.crypted/signed", "Message is not crypted/signed");
        
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
        
        var desData = await GetDesData(attachments.Value, privateKey, cancellationToken);
        if (desData.IsFailure)
            return desData.Errors;

        if (!string.IsNullOrWhiteSpace(message.Value.Body) && message.Value.IsCrypted)
        {
            var body =  DecryptBody(desData.Value.key, desData.Value.iv,message.Value);

            if (body.IsFailure)
                return body.Errors;

            message.Value.Body = body.Value;
        }
        
        _logger.LogInformation("User {email} got message from folder {folder}",
            query.MailCredentialsDto.Email, query.EmailFolder);
        
        return message;
    }

    /// <summary>
    /// Расшифровка тела письма
    /// </summary>
    /// <param name="iv">Вектор инициализации</param>
    /// <param name="message">Письмо</param>
    /// <param name="key">Ключ</param>
    /// <returns></returns>
    private Result<string> DecryptBody(byte[] key, byte[] iv, Letter message)
    {
        var decryptedBody = _desCryptProvider.Decrypt(
            Convert.FromBase64String(message.Body!), key, iv);

        if (decryptedBody.IsFailure)
            return decryptedBody.Errors;
        
        return Encoding.UTF8.GetString(decryptedBody.Value);
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