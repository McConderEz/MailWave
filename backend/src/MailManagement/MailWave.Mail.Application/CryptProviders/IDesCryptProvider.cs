using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.CryptProviders;

public interface IDesCryptProvider
{
    /// <summary>
    /// Шифрование данных алгоритмом DES
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <returns></returns>
    Result<string> Encrypt(string inputData, string key, string iv);

    /// <summary>
    /// Дешифрование данных алгоритмом DES
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <returns></returns>
    Result<string> Decrypt(string inputData, string key, string iv);

    /// <summary>
    /// Генерация ключей DES
    /// </summary>
    /// <returns>Пара ключ и вектор инициализации</returns>
    Result<(string key, string iv)> GenerateKey();
}