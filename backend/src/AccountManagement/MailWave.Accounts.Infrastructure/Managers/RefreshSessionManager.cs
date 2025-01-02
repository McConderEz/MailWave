using MailWave.Accounts.Application.Managers;
using MailWave.Accounts.Domain.Models;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDatabaseSettings = MailWave.Accounts.Infrastructure.Options.MongoDatabaseSettings;

namespace MailWave.Accounts.Infrastructure.Managers;

public class RefreshSessionManager: IRefreshSessionManager
{
    private readonly IMongoCollection<RefreshSession> _refreshSessionCollection;

    public RefreshSessionManager(IMongoDatabase database, IOptions<MongoDatabaseSettings> settings)
    {
        _refreshSessionCollection = database
            .GetCollection<RefreshSession>(settings.Value.RefreshSessionsCollectionName);
    }
    
    
    public async Task Delete(RefreshSession refreshSession, CancellationToken cancellationToken = default)
    {
        await _refreshSessionCollection.DeleteOneAsync(r => r.Id == refreshSession.Id,
            cancellationToken);
    }
    
    public async Task<Result> Add(RefreshSession refreshSession, CancellationToken cancellationToken = default)
    {
        await _refreshSessionCollection.InsertOneAsync(refreshSession, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<RefreshSession>> GetByRefreshToken(Guid refreshToken, CancellationToken cancellationToken = default)
    {
        var refreshSessionToken =
            await _refreshSessionCollection.Find(r => Guid.Parse(r.RefreshToken) == refreshToken)
                .ToListAsync(cancellationToken);

        if (refreshSessionToken.Count != 1)
            return Errors.General.NotFound();

        return refreshSessionToken.First();
    }
}