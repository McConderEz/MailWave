using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Core.Models;

public record ResponseError(string? ErrorCode, string? ErrorMessage, string? InvalidField);

/// <summary>
/// Паттерн конверт для ответа на отправленные запросы
/// </summary>
public record Envelope
{
    /// <summary>
    /// Результирующий объект
    /// </summary>
    public object? Result { get; }
    
    /// <summary>
    /// Список ошибок
    /// </summary>
    public ErrorList? Errors { get; }
    
    /// <summary>
    /// Сгенерированное время ответа
    /// </summary>
    public DateTime TimeGenerated { get; }

    private Envelope(object? result, ErrorList errors)
    {
        Result = result;
        Errors = errors;
        TimeGenerated = DateTime.Now;
    }

    /// <summary>
    /// Успешная обработка запроса
    /// </summary>
    /// <param name="result">Результат обработчиков, т.е. какой-то объект</param>
    /// <returns>Объект, обёрнутый в Envelope</returns>
    public static Envelope Ok(object? result = null) =>
        new(result, null);

    /// <summary>
    /// Ошибка при обработке запроса
    /// </summary>
    /// <param name="errors">Список ошибок, полученных в обработчиках</param>
    /// <returns>Ошибки,обёрнутые в Envelope</returns>
    public static Envelope Error(ErrorList errors) =>
        new(null, errors);
}
