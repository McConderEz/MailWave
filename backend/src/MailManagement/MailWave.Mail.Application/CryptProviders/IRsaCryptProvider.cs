using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.CryptProviders;

public interface IRsaCryptProvider
{
    /// <summary>
    /// Шифрование данных алгоритмом RSA
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="publicKey">Публичный ключ для шифрования</param>
    /// <returns></returns>
    Result<byte[]> Encrypt(string inputData, byte[] publicKey);

    /// <summary>
    /// Дешифрование данных алгоритмом RSA
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="privateKey">Приватный ключ для дешифрования</param>
    /// <returns></returns>
    Result<byte[]> Decrypt(string inputData, byte[] privateKey);

    /// <summary>
    /// Генерация ключей RSA
    /// </summary>
    /// <returns>Публичный и приватный ключ</returns>
    (byte[] publicKey, byte[] privateKey) GenerateKey();

    /// <summary>
    /// Подпись данных RSA с помощью MD5
    /// </summary>
    /// <param name="hashData">Хэш данных</param>
    /// <param name="privateKey">Приватный ключ для ЭЦП</param>
    /// <returns></returns>
    Result<string> Sign(string hashData, string privateKey);

    /// <summary>
    /// Проверка ЭЦП RSA
    /// </summary>
    /// <param name="inputData">Проверяемые данные</param>
    /// <param name="signature">Сигнатура</param>
    /// <param name="publicKey">Публичный ключ</param>
    /// <returns></returns>
    Result<bool> Verify(string inputData, string signature, string publicKey);
}