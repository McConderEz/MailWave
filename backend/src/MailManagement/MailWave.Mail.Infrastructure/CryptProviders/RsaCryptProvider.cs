using System.Security.Cryptography;
using System.Text;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Infrastructure.CryptProviders;

public class RsaCryptProvider
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
    public Result<string> Encrypt(string inputData, string publicKey)
    {
        try
        {
            using var rsa = new RSACryptoServiceProvider();

            rsa.ImportRSAPublicKey(Encoding.UTF8.GetBytes(publicKey),out _);
            
            var bytesData = Encoding.UTF8.GetBytes(inputData);
            
            return Encoding.UTF8.GetString(rsa.Encrypt(bytesData, false));
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
    public Result<string> Decrypt(string inputData, string privateKey)
    {
        try
        {
            using var rsa = new RSACryptoServiceProvider();

            rsa.ImportRSAPrivateKey(Encoding.UTF8.GetBytes(privateKey),out _);
            
            var bytesData = Encoding.UTF8.GetBytes(inputData);
            
            return Encoding.UTF8.GetString(rsa.Decrypt(bytesData, false));
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
    public Result<(string publicKey, string privateKey)> GenerateKey()
    {
        using var rsa = new RSACryptoServiceProvider(2048);

        var publicKey = Encoding.UTF8.GetString(rsa.ExportRSAPublicKey());
        var privateKey = Encoding.UTF8.GetString(rsa.ExportRSAPrivateKey());
        
        return (publicKey, privateKey);
    }

    /// <summary>
    /// Подпись данных RSA с помощью MD5
    /// </summary>
    /// <param name="hashData">Хэш данных</param>
    /// <param name="privateKey">Приватный ключ для ЭЦП</param>
    /// <returns></returns>
    public Result<string> Sign(string hashData, string privateKey)
    {
        try
        {
            using var rsa = new RSACryptoServiceProvider(2048);

            rsa.ImportRSAPrivateKey(Encoding.UTF8.GetBytes(privateKey), out _);

            var bytesData = Encoding.UTF8.GetBytes(hashData);

            return Encoding.UTF8.GetString(
                rsa.SignData(bytesData, HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1));
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
    public Result<bool> Verify(string inputData, string signature, string publicKey)
    {
        try
        {
            using var rsa = new RSACryptoServiceProvider();

            rsa.ImportRSAPublicKey(Encoding.UTF8.GetBytes(publicKey), out _);

            return rsa.VerifyData(
                Encoding.UTF8.GetBytes(inputData),
                Encoding.UTF8.GetBytes(signature),
                HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong with verification. Ex. message: {ex}", ex.Message);
            return Error.Failure("verify.error", "Cannot verify data by rsa");
        }
    }
}