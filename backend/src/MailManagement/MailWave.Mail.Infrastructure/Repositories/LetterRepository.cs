using MailWave.Mail.Application.Repositories;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace MailWave.Mail.Infrastructure.Repositories;

/// <summary>
/// Репозиторий писем
/// </summary>
public class LetterRepository : ILetterRepository
{
    private readonly ApplicationDbContext _dbContext;

    public LetterRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Добавление письма в БД
    /// </summary>
    /// <param name="letter">Коллекция писем</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task Add(IEnumerable<Letter> letter, CancellationToken cancellationToken = default)
    {
        await _dbContext.Letters.AddRangeAsync(letter, cancellationToken);
    }

    /// <summary>
    /// Удаление письма по id
    /// </summary>
    /// <param name="folderName">Название папки</param>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="emailPrefix">Домен почты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> Delete(
        string folderName, 
        uint id,
        string emailPrefix,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Letters
            .FirstOrDefaultAsync(l =>
                l.Id == id && l.Folder == folderName && l.EmailPrefix == emailPrefix, cancellationToken);
        if (item is null)
            return Errors.General.NotFound();
        
        _dbContext.Letters.Remove(item);
        
        return Result.Success();
    }

    /// <summary>
    /// Получение письма из БД по id
    /// </summary>
    /// <param name="folderName">Название папки</param>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="emailPrefix">Домен почты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result<Letter>> GetById(
        string folderName,
        uint id,
        string emailPrefix,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Letters
            .FirstOrDefaultAsync(l =>
                l.Id == id && l.Folder == folderName && l.EmailPrefix == emailPrefix, cancellationToken);
        if (item is null)
            return Errors.General.NotFound();

        return item;
    }
    
    /// <summary>
    /// Получение писем из БД по идентификаторам 
    /// </summary>
    /// <param name="ids">Коллекция уникальных идентификаторов</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result<List<Letter>>> GetByIds(IEnumerable<uint> ids, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Letters.Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken);
        
        return item;
    }

    /// <summary>
    /// Получение писем из папки с пагинацией
    /// </summary>
    /// <param name="folderName">Название папки</param>
    /// <param name="emailPrefix">Домен почты</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result<List<Letter>>> GetByFolder(
        string folderName,
        string emailPrefix,
        int page, 
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var letters = await _dbContext.Letters.Where(
                l => l.Folder == folderName && l.EmailPrefix == emailPrefix)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderByDescending(l => l.Date)
            .ToListAsync(cancellationToken);

        return letters;
    }

    /// <summary>
    /// Получение писем по учётным данным с пагинацией
    /// </summary>
    /// <param name="email">Имя пользователя</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<List<Letter>> GetByCredentialsWithPagination(
        string email,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var letters = await _dbContext.Letters
            .FromSqlRaw(@"
        SELECT * FROM mail.letters 
        WHERE EXISTS (
            SELECT 1 FROM jsonb_array_elements_text(""to"") AS t 
            WHERE t = @email
        ) OR ""from"" = @email 
        ORDER BY date DESC 
        LIMIT @pageSize OFFSET @offset", 
                new NpgsqlParameter("@email", email),
                new NpgsqlParameter("@pageSize", pageSize),
                new NpgsqlParameter("@offset", (page - 1) * pageSize))
            .ToListAsync(cancellationToken);

        return letters;
    }
    
    /// <summary>
    /// Получение сохранённого письма из бд
    /// </summary>
    /// <param name="email">Имя пользователя</param>
    /// <param name="messageId">Идентификатор письма</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Letter> GetByCredentialsAndId(
        string email,
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        //TODO: сделать в конфигурации letter колонку Id в snake case
        var letter = await _dbContext.Letters
            .FromSqlRaw(@"
        SELECT * FROM mail.letters 
        WHERE EXISTS (
            SELECT 1 FROM jsonb_array_elements_text(""to"") AS t 
            WHERE t = @email
        ) OR ""from"" = @email 
        AND ""Id"" = @messageId
        LIMIT 1",
                new NpgsqlParameter("@email", email),
                new NpgsqlParameter("@messageId", NpgsqlDbType.Integer) { Value = (int)messageId })
            .ToListAsync(cancellationToken);

        return letter.FirstOrDefault()!;
    }
}