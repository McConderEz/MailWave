﻿using System.Security.Cryptography;
using System.Text;
using MailWave.Mail.Application.CryptProviders;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Infrastructure.CryptProviders;

/// <summary>
/// Провайдер для шифрования и подписи алгоритмом RSA
/// </summary>
public class RsaCryptProvider : IRsaCryptProvider
{
    private readonly ILogger<RsaCryptProvider> _logger;

    public RsaCryptProvider(ILogger<RsaCryptProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Шифрование данных алгоритмом RSA
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="publicKey">Публичный ключ для шифрования</param>
    /// <returns></returns>
    public Result<byte[]> Encrypt(string inputData, byte[] publicKey)
    {
        try
        {
            using var rsa = new RSACryptoServiceProvider();

            rsa.ImportRSAPublicKey(publicKey,out _);
            
            var bytesData = Convert.FromBase64String(inputData);
            
            return rsa.Encrypt(bytesData, false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong with crypt. Ex. message: {ex}", ex.Message);
            return Error.Failure("encrypt.error", "Cannot encrypt data by rsa");
        }
    }

    /// <summary>
    /// Дешифрование данных алгоритмом RSA
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="privateKey">Приватный ключ для дешифрования</param>
    /// <returns></returns>
    public Result<byte[]> Decrypt(string inputData, byte[] privateKey)
    {
        try
        {
            using var rsa = new RSACryptoServiceProvider();

            rsa.ImportRSAPrivateKey(privateKey,out _);
            
            var bytesData = Convert.FromBase64String(inputData);
            
            return rsa.Decrypt(bytesData, false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong with decrypt. Ex. message: {ex}", ex.Message);
            return Error.Failure("decrypt.error", "Cannot decrypt data by rsa");
        }
    }

    /// <summary>
    /// Генерация ключей RSA
    /// </summary>
    /// <returns>Публичный и приватный ключ</returns>
    public (byte[] publicKey, byte[] privateKey) GenerateKey()
    {
        using var rsa = new RSACryptoServiceProvider(2048);

        return (rsa.ExportRSAPublicKey(), rsa.ExportRSAPrivateKey());
    }

    /// <summary>
    /// Подпись данных RSA с помощью MD5
    /// </summary>
    /// <param name="hashData">Хэш данных</param>
    /// <param name="privateKey">Приватный ключ для ЭЦП</param>
    /// <returns></returns>
    public Result<byte[]> Sign(string hashData, byte[] privateKey)
    {
        try
        {
            using var rsa = new RSACryptoServiceProvider(2048);

            rsa.ImportRSAPrivateKey(privateKey, out _);

            var bytesData = Encoding.UTF8.GetBytes(hashData);

            return rsa.SignData(bytesData, HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong with signification. Ex. message: {ex}", ex.Message);
            return Error.Failure("sign.error", "Cannot sign data by rsa");
        }
    }

    /// <summary>
    /// Проверка ЭЦП RSA
    /// </summary>
    /// <param name="inputData">Проверяемые данные</param>
    /// <param name="signature">Сигнатура</param>
    /// <param name="publicKey">Публичный ключ</param>
    /// <returns></returns>
    public Result<bool> Verify(string inputData, byte[] signature, byte[] publicKey)
    {
        try
        {
            using var rsa = new RSACryptoServiceProvider();

            rsa.ImportRSAPublicKey(publicKey, out _);

            return rsa.VerifyData(
                Encoding.UTF8.GetBytes(inputData),
                signature,
                HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong with verification. Ex. message: {ex}", ex.Message);
            return Error.Failure("verify.error", "Cannot verify data by rsa");
        }
    }
}