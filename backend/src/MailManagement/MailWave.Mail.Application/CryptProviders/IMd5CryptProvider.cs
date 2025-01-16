using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.CryptProviders;

public interface IMd5CryptProvider
{
    /// <summary>
    /// Вычисления хэша через MD5
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <returns></returns>
    Result<string> ComputeHash(byte[] inputData);
}