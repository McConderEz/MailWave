using MailWave.Accounts.Domain.Models;

namespace MailWave.Accounts.Application.Repositories;

public interface IFriendshipRepository
{
    Task<List<Friendship>?> GetByEmail(string email, CancellationToken cancellationToken = default);
    Task Delete(string friendshipId, CancellationToken cancellationToken = default);
    Task Add(Friendship friendship, CancellationToken cancellationToken = default);
}