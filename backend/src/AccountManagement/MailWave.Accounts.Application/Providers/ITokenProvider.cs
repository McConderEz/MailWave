using MailWave.Accounts.Application.Models;
using MailWave.Accounts.Domain.Models;

namespace MailWave.Accounts.Application.Providers;

public interface ITokenProvider
{
    JwtTokenResult GenerateAccessToken(User user, CancellationToken cancellationToken = default);
    Task<Guid> GenerateRefreshToken(User user, Guid accessTokenJti, CancellationToken cancellationToken = default);
}