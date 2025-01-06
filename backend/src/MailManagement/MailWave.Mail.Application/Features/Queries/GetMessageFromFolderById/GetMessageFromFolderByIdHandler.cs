using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Queries.GetMessageFromFolderById;

/// <summary>
/// Получение письма из папки по message id
/// </summary>
public class GetMessageFromFolderByIdHandler: IQueryHandler<Letter, GetMessageFromFolderByIdQuery>
{
    private readonly IValidator<GetMessageFromFolderByIdQuery> _validator;
    private readonly ILogger<GetMessageFromFolderByIdHandler> _logger;
    private readonly IMailService _mailService;

    public GetMessageFromFolderByIdHandler(
        IValidator<GetMessageFromFolderByIdQuery> validator,
        ILogger<GetMessageFromFolderByIdHandler> logger,
        IMailService mailService)
    {
        _validator = validator;
        _logger = logger;
        _mailService = mailService;
    }
    
    /// <summary>
    /// Обработчик
    /// </summary>
    /// <param name="query">Запрос со всеми входными параметрами</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список писем</returns>
    public async Task<Result<Letter>> Handle(GetMessageFromFolderByIdQuery query, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var messages = await _mailService.GetMessage(
            query.MailCredentialsDto,
            query.EmailFolder,
            query.MessageId,
            cancellationToken);

        if (messages.IsFailure)
            return messages.Errors;

        _logger.LogInformation("User {email} got message from folder {folder}",
            query.MailCredentialsDto.Email, query.EmailFolder);
        
        return messages;
    }
}