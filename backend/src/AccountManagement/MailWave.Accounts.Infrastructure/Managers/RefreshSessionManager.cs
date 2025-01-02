using MailWave.Accounts.Application.Managers;
using MailWave.Accounts.Domain.Models;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using MongoDB.Driver;

namespace MailWave.Accounts.Infrastructure.Managers;

public class RefreshSessionManager: IRefreshSessionManager
{
    private readonly AccountDbContext _accountDbContext;

    public RefreshSessionManager(AccountDbContext accountDbContext)
    {
        _accountDbContext = accountDbContext;
    }
    
    
    public async Task Delete(RefreshSession refreshSession, CancellationToken cancellationToken = default)
    {
        await _accountDbContext.RefreshSessionCollection.DeleteOneAsync(r => r.Id == refreshSession.Id,
            cancellationToken);
    }
    
    public async Task<Result> Add(RefreshSession refreshSession, CancellationToken cancellationToken = default)
    {
        await _accountDbContext.RefreshSessionCollection.InsertOneAsync(refreshSession, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<RefreshSession>> GetByRefreshToken(Guid refreshToken, CancellationToken cancellationToken = default)
    {
        var refreshSessionToken =
            await _accountDbContext.RefreshSessionCollection.Find(r => r.RefreshToken == refreshToken)
                .ToListAsync(cancellationToken);

        if (refreshSessionToken.Count != 1)
            return Errors.General.NotFound();

        return refreshSessionToken.First();
    }
}