using System.Data;

namespace MailWave.Mail.Application;

public interface IUnitOfWork
{
    /// <summary>
    /// Открытие транзакции
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<IDbTransaction> BeginTransaction(CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранение изменений
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}