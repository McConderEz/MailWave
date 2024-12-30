using MailWave.Accounts.Domain.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDatabaseSettings = MailWave.Accounts.Infrastructure.Options.MongoDatabaseSettings;

namespace MailWave.Accounts.Infrastructure;

public class AccountDbContext
{
    private readonly MongoDatabaseSettings _options;
    private readonly IMongoDatabase _database;

    public AccountDbContext(IOptions<MongoDatabaseSettings> options)
    {
        _options = options.Value;
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<User> UserCollection =>
        _database.GetCollection<User>(_options.UsersCollectionName);
    
    public IMongoCollection<RefreshSession> RefreshSessionCollection =>
        _database.GetCollection<RefreshSession>(_options.RefreshSessionsCollectionName);
}