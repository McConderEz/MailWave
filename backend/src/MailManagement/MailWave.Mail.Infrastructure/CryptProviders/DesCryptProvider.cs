using System.Security.Cryptography;
using System.Text;
using MailWave.Mail.Application.CryptProviders;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Infrastructure.CryptProviders;

/// <summary>
/// Провайдер для шифрования данных DES алгоритмом
/// </summary>
public class DesCryptProvider : IDesCryptProvider
{
    private readonly ILogger<DesCryptProvider> _logger;

    public DesCryptProvider(ILogger<DesCryptProvider> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Шифрование данных алгоритмом DES
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <returns></returns>
    public Result<byte[]> Encrypt(byte[] inputData, byte[] key, byte[] iv)
    {
        try
        {
            using var des = DES.Create();
            
            des.Key = key;
            des.IV = iv;
            des.Padding = PaddingMode.PKCS7;
            
            return des.EncryptCfb(inputData, iv);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong with crypt. Ex. message: {ex}", ex.Message);
            return Error.Failure("encrypt.error", "Cannot encrypt data by des");
        }
    }
    
    /// <summary>
    /// Дешифрование данных алгоритмом DES
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <returns></returns>
    public Result<byte[]> Decrypt(byte[] inputData, byte[] key, byte[] iv)
    {
        try
        {
            using var des = DES.Create();
            
            des.Key = key;
            des.IV = iv;
            des.Padding = PaddingMode.PKCS7;
            
            return des.DecryptCfb(inputData, iv);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong with decrypt. Ex. message: {ex}", ex.Message);
            return Error.Failure("decrypt.error", "Cannot decrypt data by des");
        }
    }

    /// <summary>
    /// Генерация ключей DES
    /// </summary>
    /// <returns>Пара ключ и вектор инициализации</returns>
    public Result<(byte[] key, byte[] iv)> GenerateKey()
    {
        using var desKey = DES.Create();
        
        return (desKey.Key, desKey.IV);
    }
}