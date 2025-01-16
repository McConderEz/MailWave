using System.Security.Cryptography;
using System.Text;
using MailWave.Mail.Application.CryptProviders;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Infrastructure.CryptProviders;

/// <summary>
/// Провайдер для вычисления хэша данных через MD5
/// </summary>
public class Md5CryptProvider : IMd5CryptProvider
{
    /// <summary>
    /// Вычисления хэша через MD5
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <returns></returns>
    public Result<string> ComputeHash(byte[] inputData)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(inputData);

        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }
}