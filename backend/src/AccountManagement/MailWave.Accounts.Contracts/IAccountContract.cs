namespace MailWave.Accounts.Contracts;

public interface IAccountContract
{
    public Task<bool> IsExistFriendShip(
        string firstUserEmail,string secondUserEmail, CancellationToken cancellationToken = default);
    
    public Task<(string publicKey, string privateKey)> GetCryptData(
        string firstUserEmail,string secondUserEmail, CancellationToken cancellationToken = default);
}