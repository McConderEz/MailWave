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

    /// <summary>
    /// Сохранение писем в базу данных
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="messageIds">Коллекция уникальных идентификаторов</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> SaveMessagesInDataBase(
        MailCredentialsDto mailCredentialsDto,
        IEnumerable<uint> messageIds,
        EmailFolder selectedFolder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправка запланированного сообщения
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи</param>
    /// <param name="attachments">Вложения</param>
    /// <param name="letter">Письмо для отправки(адреса получателей, отправитель, основная информация)</param>
    /// /// <param name="enqueueAt">Дата и время отправки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result> SendScheduledMessage(
        MailCredentialsDto mailCredentialsDto,
        IEnumerable<Attachment>? attachments,
        Letter letter,
        DateTime enqueueAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение вложений письма
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи пользователя</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="messageId">Уникальный идентификатор сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result<List<Attachment>>> GetAttachmentsOfMessage(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder,
        uint messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение общего количества писем из папки
    /// </summary>
    /// <param name="mailCredentialsDto">Данные учётной записи пользователя</param>
    /// <param name="selectedFolder">Выбранная папка</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<Result<int>> GetMessagesCountFromFolder(
        MailCredentialsDto mailCredentialsDto,
        EmailFolder selectedFolder,
        CancellationToken cancellationToken = default);
}