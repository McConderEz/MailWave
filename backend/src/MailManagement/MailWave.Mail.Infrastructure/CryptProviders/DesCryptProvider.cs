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
    public Result<string> Encrypt(string inputData, string key, string iv)
    {
        try
        {
            using var des = DES.Create();

            var bytesIv = Convert.FromBase64String(iv);
            
            des.Key = Convert.FromBase64String(key);
            des.IV = bytesIv;
            des.Padding = PaddingMode.Zeros;
            
            var bytesData = Encoding.UTF8.GetBytes(inputData);
            
            return Convert.ToBase64String(des.EncryptCfb(bytesData, bytesIv));
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
    public Result<string> Decrypt(string inputData, string key, string iv)
    {
        try
        {
            using var des = DES.Create();
            
            var bytesIv = Encoding.UTF8.GetBytes(iv);
            
            des.Key = Encoding.UTF8.GetBytes(key);
            des.IV = bytesIv;
            des.Padding = PaddingMode.Zeros;

            var bytesData = Encoding.UTF8.GetBytes(inputData);
            
            return Convert.ToBase64String(des.DecryptCfb(bytesData, bytesIv));
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
    public Result<(string key, string iv)> GenerateKey()
    {
        using var desKey = DES.Create();

        var key = Convert.ToBase64String(desKey.Key);
        var iv = Convert.ToBase64String(desKey.IV);
        
        return (key, iv);
    }
}