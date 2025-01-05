using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Queries.GetMessagesFromFolderWithPagination;

public class GetMessagesFromFolderWithPaginationHandler: IQueryHandler<List<Letter>,GetMessagesFromFolderWithPaginationQuery>
{
    private readonly IValidator<GetMessagesFromFolderWithPaginationQuery> _validator;
    private readonly ILogger<GetMessagesFromFolderWithPaginationHandler> _logger;
    private readonly IMailService _mailService;

    public GetMessagesFromFolderWithPaginationHandler(
        IValidator<GetMessagesFromFolderWithPaginationQuery> validator,
        ILogger<GetMessagesFromFolderWithPaginationHandler> logger,
        IMailService mailService)
    {
        _validator = validator;
        _logger = logger;
        _mailService = mailService;
    }


    public async Task<Result<List<Letter>>> Handle(
        GetMessagesFromFolderWithPaginationQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var messages = await _mailService.GetMessages(
            query.MailCredentialsDto,
            query.EmailFolder,
            query.Page,
            query.PageSize,
            cancellationToken);

        if (messages.IsFailure)
            return messages.Errors;

        _logger.LogInformation("User {email} got messages from folder", query.EmailFolder);
        
        return messages;
    }
}