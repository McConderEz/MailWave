namespace MailWave.SharedKernel.Shared.Errors;

/// <summary>
/// Паттерн ошибка
/// </summary>
public class Error
{
    private const string SEPARATOR = "||";
    
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    
    /// <summary>
    /// Код ошибки
    /// </summary>
    public string ErrorCode { get; } 
    
    /// <summary>
    /// Сообщение об ошибке
    /// </summary>
    public string ErrorMessage { get; }
    
    /// <summary>
    /// Тип ошибки
    /// </summary>
    public ErrorType Type { get; }
    
    /// <summary>
    /// Невалидное поле, из-за которого произошла ошибка
    /// </summary>
    public string? InvalidField { get; } = null;

    private Error(string errorCode, string errorMessage, ErrorType type, string? invalidField = null)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Type = type;
        InvalidField = invalidField;
    }
    
    /// <summary>
    /// Сериализация ошибки для FluentValidator
    /// </summary>
    /// <returns>Возвращает ошибки с разделителем</returns>
    public string Serialize()
    {
        return string.Join(SEPARATOR, ErrorCode, ErrorMessage, Type);
    }

    /// <summary>
    /// Десериализация ошибки с разделителем
    /// </summary>
    /// <param name="serialized">Сериализованная ошибка</param>
    /// <returns>Ошибка в привычном виде</returns>
    /// <exception cref="ArgumentException">Исключительные ситуации неверного формата для десериализации</exception>
    public static Error Deserialize(string serialized)
    {
        var parts = serialized.Split(SEPARATOR);

        if (parts.Length < 3)
        {
            throw new ArgumentException("Invalid serialized format");
        }

        if (Enum.TryParse<ErrorType>(parts[2], out var type) == false)
        {
            throw new ArgumentException("Invalid serialized format");
        }

        return new Error(parts[0], parts[1], type);
    }

    public static Error Validation(string errorCode, string errorMessage, string? invalidField = null) =>
        new(errorCode, errorMessage, ErrorType.Validation, invalidField);
    
    public static Error Failure(string errorCode, string errorMessage) =>
        new(errorCode, errorMessage, ErrorType.Failure);
    
    public static Error NotFound(string errorCode, string errorMessage) =>
        new(errorCode, errorMessage, ErrorType.NotFound);
    
    public static Error Conflict(string errorCode, string errorMessage) =>
        new(errorCode, errorMessage, ErrorType.Conflict);
    
    public static Error Null(string errorCode, string errorMessage) =>
        new(errorCode, errorMessage, ErrorType.Null);

    public ErrorList ToErrorList() => new([this]);
    
    public override string ToString()
    {
        return $"ErrorCode: {ErrorCode}.\nErrorMessage:{ErrorMessage}\n{Type}";
    }
}

public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Failure,
    Null,
    Conflict
}