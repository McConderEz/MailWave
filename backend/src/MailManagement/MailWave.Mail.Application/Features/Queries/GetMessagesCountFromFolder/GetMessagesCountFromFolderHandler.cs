using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Queries.GetMessagesCountFromFolder;

/// <summary>
/// Получение общего количества записей из папки
/// </summary>
public class GetMessagesCountFromFolderHandler: IQueryHandler<int,GetMessagesCountFromFolderQuery>
{
    private readonly IValidator<GetMessagesCountFromFolderQuery> _validator;
    private readonly ILogger<GetMessagesCountFromFolderHandler> _logger;
    private readonly IMailService _mailService;

    public GetMessagesCountFromFolderHandler(
        IValidator<GetMessagesCountFromFolderQuery> validator,
        ILogger<GetMessagesCountFromFolderHandler> logger,
        IMailService mailService)
    {
        _validator = validator;
        _logger = logger;
        _mailService = mailService;
    }

    public async Task<Result<int>> Handle(
        GetMessagesCountFromFolderQuery query, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var messages = await _mailService.GetMessagesCountFromFolder(
            query.MailCredentialsDto,
            query.EmailFolder,
            cancellationToken);

        if (messages.IsFailure)
            return messages.Errors;

        _logger.LogInformation("User {email} got messages count from folder {folder}",
            query.MailCredentialsDto.Email, query.EmailFolder);
        
        return messages;
    }
}