using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using EmailFolder = MailWave.Mail.Domain.Constraints.Constraints.EmailFolder;

namespace MailWave.Mail.Application.MailService;

public interface IMailService
{
    /// <summary>
    /// Метод отправки данных по почте
    /// </summary>
    /// <param name="letter">Письмо для отправки(адреса получателей, отправитель, основная информация)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> SendMessage(Letter letter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получения писем из папки
    /// </summary>
    /// <param name="selectedFolder">Папка, из которой получаем</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Result со списком писем</returns>
    Task<Result<List<Letter>>> GetMessages(
        EmailFolder selectedFolder, 
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение письма по идентификатору
    /// </summary>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Идентификатор письма uid</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Письмо с вложениями</returns>
    Task<Result<Letter>> GetMessage(
        EmailFolder selectedFolder, 
        uint messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаление письма из выбранной папки по уникальному идентификатору
    /// </summary>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Идентификатор письма uid</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> DeleteMessage(
        EmailFolder selectedFolder,
        uint messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Перемещение письма из выбранной папки в целевую по уникальному идентификатору 
    /// </summary>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="targetFolder">Папка для перемещения</param>
    /// <param name="messageId">Идентификатор письма</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> MoveMessage(
        EmailFolder selectedFolder,
        EmailFolder targetFolder,
        uint messageId,
        CancellationToken cancellationToken = default);
}