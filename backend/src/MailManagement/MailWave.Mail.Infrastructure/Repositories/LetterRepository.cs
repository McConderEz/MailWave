using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace MailWave.Mail.Infrastructure.Repositories;

/// <summary>
/// Репозиторий писем
/// </summary>
public class LetterRepository
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
}