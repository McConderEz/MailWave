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

namespace MailWave.Mail.Application.Features.Commands.SaveFiles;

/// <summary>
/// Сохранение вложений
/// </summary>
public class SaveFilesHandler: ICommandHandler<SaveFilesCommand>
{
    private readonly IValidator<SaveFilesCommand> _validator;
    private readonly ILogger<SaveFilesHandler> _logger;
    private readonly IDesCryptProvider _desCryptProvider;
    private readonly IRsaCryptProvider _rsaCryptProvider;
    private readonly IAccountContract _accountContract;
    private readonly IMailService _mailService;

    public SaveFilesHandler(
        IValidator<SaveFilesCommand> validator,
        ILogger<SaveFilesHandler> logger,
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
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> Handle(SaveFilesCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();
        
        var message = await _mailService.GetMessage(
            command.MailCredentialsDto,
            command.EmailFolder,
            command.MessageId,
            cancellationToken);

        if (message.IsFailure)
            return message.Errors;
        
        var attachments = await _mailService.GetAttachmentsOfMessage(
            command.MailCredentialsDto,
            command.EmailFolder,
            command.MessageId,
            cancellationToken);

        if (attachments.IsFailure)
            return attachments.Errors;
        
        var (publicKey, privateKey) = await _accountContract.GetCryptData(
            command.MailCredentialsDto.Email,
            message.Value.From,
            cancellationToken);
        
        if (publicKey == string.Empty || privateKey == string.Empty)
            return Errors.MailErrors.NotFriendError();
        
        var desData = await GetDesData(attachments.Value, privateKey, cancellationToken);
        if (desData.IsFailure)
            return desData.Errors;

        if (message.Value is { IsCrypted: true })
        { 
            await DecryptAndSaveAttachments(
                command.DirectoryPath,
                command.FileName,
                attachments.Value,
                desData.Value.key,
                desData.Value.iv,
                cancellationToken);
        }
        else
        {
            await SaveAttachments(
                command.DirectoryPath, 
                command.FileName,
                attachments.Value, 
                cancellationToken);
        }
        
        attachments.Value.ForEach(a => a.Content.Close());
     
        _logger.LogInformation("Data`s saved in {directory}", command.DirectoryPath);
        
        return Result.Success();
    }

    /// <summary>
    /// Сохранение вложений
    /// </summary>
    /// <param name="directoryPath">Директория для сохранения</param>
    /// <param name="fileName">Название файла</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    private async Task SaveAttachments(
        string directoryPath,
        string fileName,
        List<Attachment> attachments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var attachment in attachments)
            {
                if (attachment.FileName != fileName) continue;
                
                using var memoryStream = new MemoryStream();

                await using var fs = new FileStream(Path.Combine(directoryPath, attachment.FileName),
                    FileMode.OpenOrCreate);

                await attachment.Content.CopyToAsync(memoryStream, cancellationToken);

                await fs.WriteAsync(memoryStream.ToArray(), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Fail to save files. Ex. msg: {ex}", ex.Message);
        }
    }

    /// <summary>
    /// Расшифровка и сохранение вложений
    /// </summary>
    /// <param name="directoryPath">Директория для сохранения</param>
    /// <param name="fileName">Название файла</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    private async Task DecryptAndSaveAttachments(
        string directoryPath,
        string fileName,
        List<Attachment> attachments,
        byte[] key,
        byte[] iv,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var attachment in attachments)
            {
                if (attachment.FileName != fileName) continue;
                
                using var memoryStream = new MemoryStream();

                if (attachment.FileName.EndsWith(".key") || attachment.FileName.EndsWith(".iv"))
                    continue;

                await attachment.Content.CopyToAsync(memoryStream, cancellationToken);

                var data = memoryStream.ToArray();

                var decryptedData = _desCryptProvider.Decrypt(data, key, iv);

                if (decryptedData.IsFailure) return;

                await using var fs = new FileStream(Path.Combine(directoryPath, attachment.FileName),
                    FileMode.OpenOrCreate);

                await fs.WriteAsync(decryptedData.Value.ToArray(), cancellationToken);
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
        try
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
        catch (Exception ex)
        {
            _logger.LogError("Failure. Ex. msg: {ex}", ex.Message);

            return Error.Failure("get.des.failure", "Fail to get des");
        }
    }
}