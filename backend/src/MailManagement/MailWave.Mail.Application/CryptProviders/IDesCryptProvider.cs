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
    Result<byte[]> Encrypt(string inputData, byte[] key, byte[] iv);

    /// <summary>
    /// Дешифрование данных алгоритмом DES
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="key">Ключ</param>
    /// <param name="iv">Вектор инициализации</param>
    /// <returns></returns>
    Result<byte[]> Decrypt(string inputData, byte[] key, byte[] iv);

    /// <summary>
    /// Генерация ключей DES
    /// </summary>
    /// <returns>Пара ключ и вектор инициализации</returns>
    Result<(byte[] key, byte[] iv)> GenerateKey();
}