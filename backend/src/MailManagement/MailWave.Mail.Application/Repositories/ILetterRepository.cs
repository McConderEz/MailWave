using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Repositories;

public interface ILetterRepository
{
    /// <summary>
    /// Добавление письма в БД
    /// </summary>
    /// <param name="letter">Коллекция писем</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task Add(IEnumerable<Letter> letter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаление письма по id
    /// </summary>
    /// <param name="folderName">Название папки</param>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="emailPrefix">Домен почты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> Delete(
        string folderName, 
        uint id,
        string emailPrefix,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение письма из БД по id
    /// </summary>
    /// <param name="folderName">Название папки</param>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="emailPrefix">Домен почты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result<Letter>> GetById(
        string folderName,
        uint id,
        string emailPrefix,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение писем из БД по идентификаторам 
    /// </summary>
    /// <param name="ids">Коллекция уникальных идентификаторов</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result<List<Letter>>> GetByIds(IEnumerable<uint> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение писем из папки с пагинацией
    /// </summary>
    /// <param name="folderName">Название папки</param>
    /// <param name="emailPrefix">Домен почты</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result<List<Letter>>> GetByFolder(
        string folderName,
        string emailPrefix,
        int page, 
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение писем по учётным данным с пагинацией
    /// </summary>
    /// <param name="email">Имя пользователя</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<List<Letter>> GetByCredentialsWithPagination(
        string email,
        int page, 
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение сохранённого письма из бд
    /// </summary>
    /// <param name="email">Имя пользователя</param>
    /// <param name="messageId">Идентификатор письма</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Letter> GetByCredentialsAndId(
        string email,
        uint messageId,
        CancellationToken cancellationToken = default);
}