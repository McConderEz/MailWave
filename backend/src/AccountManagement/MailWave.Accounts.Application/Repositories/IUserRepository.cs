using MailWave.Accounts.Domain.Models;

namespace MailWave.Accounts.Application.Repositories;

public interface IUserRepository
{
    Task<List<User>> Get(IEnumerable<Guid> userIds,CancellationToken cancellationToken = default);
    Task<User?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task Add(User user, CancellationToken cancellationToken = default);
}