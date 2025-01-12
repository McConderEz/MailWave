using MailWave.Accounts.Application.Repositories;
using MailWave.Accounts.Domain.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDatabaseSettings = MailWave.Accounts.Infrastructure.Options.MongoDatabaseSettings;

namespace MailWave.Accounts.Infrastructure.Repositories;

public class FriendshipRepository: IFriendshipRepository
{
    private readonly IMongoCollection<Friendship> _friendshipCollection;

    public FriendshipRepository(IMongoDatabase database, IOptions<MongoDatabaseSettings> settings)
    {
        _friendshipCollection = database.GetCollection<Friendship>(settings.Value.FriendShipCollectionName);
    }
    
    public async Task<List<Friendship>?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        return await _friendshipCollection
            .Find(friendship => friendship.FirstUserEmail == email || friendship.SecondUserEmail == email)
            .ToListAsync(cancellationToken);
    }

    public async Task Delete(string friendshipId, CancellationToken cancellationToken = default)
    {
        await _friendshipCollection.DeleteOneAsync(friendship => friendship.Id == friendshipId, cancellationToken);
    }

    public async Task Add(Friendship friendship, CancellationToken cancellationToken = default)
    {
        await _friendshipCollection.InsertOneAsync(friendship, cancellationToken);
    }
}