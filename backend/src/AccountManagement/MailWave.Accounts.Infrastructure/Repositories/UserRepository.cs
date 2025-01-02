using System.ComponentModel.Design;
using MailWave.Accounts.Application.Repositories;
using MailWave.Accounts.Domain.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDatabaseSettings = MailWave.Accounts.Infrastructure.Options.MongoDatabaseSettings;

namespace MailWave.Accounts.Infrastructure.Repositories;

public class UserRepository: IUserRepository
{
    private readonly IMongoCollection<User> _usersCollection;

    public UserRepository(IMongoDatabase database, IOptions<MongoDatabaseSettings> settings)
    {
        _usersCollection = database.GetCollection<User>(settings.Value.UsersCollectionName);
    }

    public async Task<List<User>> Get(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var userIdsToString = userIds.Select(id => id.ToString());
        
        return await _usersCollection.Find(user => userIdsToString.Contains(user.Id.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _usersCollection.Find(user => user.Id == id.ToString()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        return await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task Add(User user, CancellationToken cancellationToken = default)
    {
        await _usersCollection.InsertOneAsync(user,cancellationToken: cancellationToken);
    }
    
}