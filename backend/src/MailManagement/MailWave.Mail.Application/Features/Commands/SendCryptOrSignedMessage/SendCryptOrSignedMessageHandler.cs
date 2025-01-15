﻿using System.Text;
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

    //TODO: Отрефакторить
    //TODO: Реализовать добавления ЭЦП к body и вложениям
    
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

        var letter = new Letter { Subject = command.Subject, To = [command.Receiver] };
        List<Attachment> attachments = [];
        
        if (command.IsCrypted)
        {
            var result = await HandleCryptedMessage(command, letter, publicKey, attachments);
            
            if (result.IsFailure)
                return result.Errors;
        }

        if (command.IsSigned)
        {
            //TODO: Реализовать
        }

        var sendingResult = await _mailService.SendMessage(
            command.MailCredentialsDto,
            attachments,
            letter,
            cancellationToken);

        return sendingResult.IsFailure ? sendingResult.Errors : Result.Success();
    }

    /// <summary>
    /// Обработка шифрованного сообщения
    /// </summary>
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="letter">Письмо</param>
    /// <param name="publicKey">Публичный ключ RSA</param>
    /// <param name="attachments">Коллекция вложений</param>
    /// <returns></returns>
    private async Task<Result> HandleCryptedMessage(
        SendCryptOrSignedMessageCommand command,
        Letter letter,
        string publicKey,
        List<Attachment> attachments)
    {
        //Генерация ключей DES
        var keys = _desCryptProvider.GenerateKey();
        if (keys.IsFailure)
            return Error.Failure("generation.keys.error", "Generation keys failure");
        
        //Проверяем наличие тела письма
        if (command.Body is not null && command.Body != String.Empty)
        {
            //Шифруем тело письма
            var result = CryptBody(command, letter, keys.Value.key, keys.Value.iv);
                
            if (result.IsFailure)
                return result.Errors;
        }

        //Проверяем наличие вложений
        if (command.AttachmentDtos?.Count() > 0)
        {
            //Шифруем вложения
            var result = await CryptAttachments(command, letter, keys.Value.key, keys.Value.iv);
                
            if (result.IsFailure)
                return result.Errors;
                
            attachments.AddRange(result.Value);
        }
        
        //Шифруем публичным RSA ключом DES ключ и вектор инициализации и прикрепляем, как вложения
        attachments.AddRange(AttachEncryptedKeyAndIv(
            keys.Value.key, keys.Value.iv, Convert.FromBase64String(publicKey)));
        
        return Result.Success();
    }

    /// <summary>
    /// Шифрование ключа и вектора инициализации RSA алгоритмом
    /// </summary>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <param name="publicKey">Публичный ключ RSA</param>
    /// <returns></returns>
    private List<Attachment> AttachEncryptedKeyAndIv(byte[] key, byte[] iv, byte[] publicKey)
    {
        //Шифрование ключа и вектора инициализации публичным RSA ключом
        var encryptedKey = _rsaCryptProvider.Encrypt(Convert.ToBase64String(key), publicKey);
        var encryptedIv = _rsaCryptProvider.Encrypt(Convert.ToBase64String(iv), publicKey);

        var attachments = new List<Attachment>
        {
            new()
            {
                FileName = "key.key",
                Content = new MemoryStream(encryptedKey.Value)
            },
            new()
            {
                FileName = "iv.iv",
                Content = new MemoryStream(encryptedIv.Value)
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
        byte[] key,
        byte[] iv)
    {
        var attachments = new List<Attachment>();
        
        //Проходим по вложениям, шифруем содержимое DES алгоритмом
        //Добавляем в коллекцию вложений физическую сущность, а в письмо название вложения
        foreach (var attachment in command.AttachmentDtos!)
        {
            using var memoryStream = new MemoryStream();
            
            await attachment.Content.CopyToAsync(memoryStream);
            
            var data = memoryStream.ToArray();

            var encryptData = _desCryptProvider.Encrypt(data, key, iv);
            if (encryptData.IsFailure)
                return encryptData.Errors;

            attachments.Add(new Attachment
            {
                FileName = attachment.FileName,
                Content = new MemoryStream(encryptData.Value)
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
    private Result CryptBody(SendCryptOrSignedMessageCommand command, Letter letter, byte[] key, byte[] iv)
    {
        //Устанавливаем флаг, что письмо зашифровано и добавляем тег в тему 
        letter.IsCrypted = true;
        letter.Subject += Domain.Constraints.Constraints.CRYPTED_SUBJECT;
        
        if (command.Body is null)
            return Error.Null("body.null", "Body is null");
        
        //Шифруем тело DES
        var body = _desCryptProvider.Encrypt(Encoding.UTF8.GetBytes(command.Body), key, iv);
        if (body.IsFailure)
            return body.Errors;

        letter.Body = Convert.ToBase64String(body.Value);
        
        _logger.LogInformation("User {email} sent crypted/signed message to {receiver}",
            command.MailCredentialsDto.Email, command.Receiver);
        
        return Result.Success();
    }
}