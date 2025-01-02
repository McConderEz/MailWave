using MailWave.Accounts.Domain.Models;
using MailWave.SharedKernel.Shared;

namespace MailWave.Accounts.Application.Managers;

public interface IRefreshSessionManager
{
    Task Delete(RefreshSession refreshSession, CancellationToken cancellationToken = default);
    Task<Result> Add(RefreshSession refreshSession, CancellationToken cancellationToken = default);
    Task<Result<RefreshSession>> GetByRefreshToken(Guid refreshToken, CancellationToken cancellationToken = default);
}