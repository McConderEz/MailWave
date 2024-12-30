using System.Runtime.InteropServices.JavaScript;
using MailWave.Core.Models;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MailWave.Framework;

/// <summary>
/// Класс расширения для ответов
/// </summary>
public static class ResponseExtensions
{
    /// <summary>
    /// Конвертация ошибки в ответ
    /// </summary>
    /// <param name="error">Ошибка</param>
    /// <returns>ActionResult</returns>
    public static ActionResult ToResponse(this Error error)
    {

        var statusCode = GetStatusCodeForErrorType(error.Type);
        
        var envelope = Envelope.Error(error);
        
        return new ObjectResult(envelope)
        {
            StatusCode = statusCode
        };
    }
    
    /// <summary>
    /// Конвертация списка ошибок в ответ
    /// </summary>
    /// <param name="errors">Список ошибок</param>
    /// <returns>ActionResult</returns>
    public static ActionResult ToResponse(this ErrorList errors)
    {

        if (!errors.Any())
            return new ObjectResult(Envelope.Error(errors))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

        var distinctErrorTypes = errors
            .Select(x => x.Type)
            .Distinct()
            .ToList();

        var statusCode = distinctErrorTypes.Count > 1
            ? StatusCodes.Status500InternalServerError
            : GetStatusCodeForErrorType(distinctErrorTypes.First());

        var envelope = Envelope.Error(errors);
        
        return new ObjectResult(envelope)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }
    
    /// <summary>
    /// Метод получения статус кода из типа ошибки
    /// </summary>
    /// <param name="errorType">Тип ошибки</param>
    /// <returns>Код ошибки</returns>
    private static int GetStatusCodeForErrorType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    
}