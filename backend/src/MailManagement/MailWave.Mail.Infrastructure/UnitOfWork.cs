using System.Data;
using MailWave.Mail.Application;
using Microsoft.EntityFrameworkCore.Storage;

namespace MailWave.Mail.Infrastructure;

/// <summary>
/// Паттерн UnitOfWork для сохранения транзакционности
/// </summary>
public class UnitOfWork: IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Открытие транзакции
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<IDbTransaction> BeginTransaction(CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        return transaction.GetDbTransaction();
    }

    /// <summary>
    /// Сохранение изменений
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
