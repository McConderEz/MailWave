using System.Text.Json.Serialization;

namespace MailWave.Mail.Domain.Entities;

/// <summary>
/// Сущность электронного письма
/// </summary>
public class Letter
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    public uint Id { get; set; }
    
    /// <summary>
    /// Отправитель письма
    /// </summary>
    public string From { get; set; } = string.Empty;
    
    /// <summary>
    /// Список получателей письма
    /// </summary>
    public List<string> To { get; set; } = [];
    
    /// <summary>
    /// Основное содержимое письма
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Названия, вложенных в письмо, файлов
    /// </summary>
    public List<string> AttachmentNames { get; set; } = [];
    
    /// <summary>
    /// Тема письма
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Папка
    /// </summary>
    public string Folder { get; set; }
    
    /// <summary>
    /// Префикс почты для составного ключа
    /// </summary>
    public string EmailPrefix { get; set; }
    
    /// <summary>
    /// Дата отправки письма
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Зашифровано ли письмо (DES,AES,TripleDES and etc.)
    /// </summary>
    public bool IsCrypted { get; set; }
    
    /// <summary>
    /// Подписано ли письмо ЭЦП (RSA, DSA)
    /// </summary>
    public bool IsSigned { get; set; }
}