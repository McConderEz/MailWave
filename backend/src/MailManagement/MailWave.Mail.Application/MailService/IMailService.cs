using MailWave.Core.DTOs;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using EmailFolder = MailWave.SharedKernel.Shared.Constraints.EmailFolder;

namespace MailWave.Mail.Application.MailService;

public interface IMailService
{
    /// <summary>
    /// Метод отправки данных по почте
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="letter">Письмо для отправки(адреса получателей, отправитель, основная информация)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> SendMessage(
        MailCredentialsDto mailCredentialsDto,
        IEnumerable<Attachment>? attachments,
        Letter letter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получения писем из папки
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="selectedFolder">Папка, из которой получаем</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Result со списком писем</returns>
    Task<Result<List<Letter>>> GetMessages(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder, 
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение письма по идентификатору
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Идентификатор письма uid</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Письмо с вложениями</returns>
    Task<Result<Letter>> GetMessage(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder, 
        uint messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаление письма из выбранной папки по уникальному идентификатору
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Идентификатор письма uid</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> DeleteMessage(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder,
        uint messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Перемещение письма из выбранной папки в целевую по уникальному идентификатору 
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="targetFolder">Папка для перемещения</param>
    /// <param name="messageId">Идентификатор письма</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> MoveMessage(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder,
        EmailFolder targetFolder,
        uint messageId,
        CancellationToken cancellationToken = default);
}