using MailWave.Accounts.Application.Repositories;
using MailWave.Accounts.Domain.Models;
using MongoDB.Driver;

namespace MailWave.Accounts.Infrastructure.Repositories;

public class UserRepository: IUserRepository
{
    private readonly AccountDbContext _context;

    public UserRepository(AccountDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> Get(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        return await _context.UserCollection.Find(user => userIds.Contains(user.Id)).ToListAsync(cancellationToken);
    }

    public async Task<User?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserCollection.Find(user => user.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task Add(User user, CancellationToken cancellationToken = default)
    {
        await _context.UserCollection.InsertOneAsync(user,cancellationToken: cancellationToken);
    }
    
}