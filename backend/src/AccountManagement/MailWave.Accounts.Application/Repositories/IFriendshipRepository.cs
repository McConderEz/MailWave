using MailWave.Accounts.Domain.Models;
using MongoDB.Bson;

namespace MailWave.Accounts.Application.Repositories;

public interface IFriendshipRepository
{
    Task<List<Friendship>?> GetByEmail(string email, CancellationToken cancellationToken = default);
    Task<Friendship?> GetByEmails(
        string firstUserEmail, string secondUserEmail, CancellationToken cancellationToken = default);
    Task Delete(string friendshipId, CancellationToken cancellationToken = default);
    Task Add(Friendship friendship, CancellationToken cancellationToken = default);

    Task Update(
        string friendshipId, BsonDocument updateSettings, CancellationToken cancellationToken = default);
}