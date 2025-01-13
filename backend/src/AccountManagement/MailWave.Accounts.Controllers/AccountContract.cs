using MailWave.Accounts.Application.Repositories;
using MailWave.Accounts.Contracts;

namespace MailWave.Accounts.Controllers;

public class AccountContract: IAccountContract
{
    private readonly IFriendshipRepository _friendshipRepository;

    public AccountContract(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }

    /// <summary>
    /// Проверка существования дружбы
    /// </summary>
    /// /// <param name="firstUserEmail">Имя первого пользователя</param>
    /// <param name="secondUserEmail">Имя второго пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<bool> IsExistFriendShip(
        string firstUserEmail,
        string secondUserEmail,
        CancellationToken cancellationToken = default)
    {
        var friendship = await _friendshipRepository.GetByEmails(
            firstUserEmail,
            secondUserEmail,
            cancellationToken);

        return friendship is not null && friendship.IsAccepted;
    }

    /// <summary>
    /// Получение публичного и приватного ключа дружбы
    /// </summary>
    /// <param name="firstUserEmail">Имя первого пользователя</param>
    /// <param name="secondUserEmail">Имя второго пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<(string publicKey, string privateKey)> GetCryptData(
        string firstUserEmail,
        string secondUserEmail,
        CancellationToken cancellationToken = default)
    {
        var friendship = await _friendshipRepository.GetByEmails(
            firstUserEmail,
            secondUserEmail,
            cancellationToken);

        var result = await IsExistFriendShip(firstUserEmail, secondUserEmail, cancellationToken);
        return !result ? (String.Empty, String.Empty) : (friendship!.PublicKey, friendship.PrivateKey);
    }
}