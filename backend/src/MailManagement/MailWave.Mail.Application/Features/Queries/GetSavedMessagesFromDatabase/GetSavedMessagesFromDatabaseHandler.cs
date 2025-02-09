using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.Repositories;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Queries.GetSavedMessagesFromDatabase;

public class GetSavedMessagesFromDatabaseHandler: IQueryHandler<List<Letter>,GetSavedMessagesFromDatabaseQuery>
{
    private readonly IValidator<GetSavedMessagesFromDatabaseQuery> _validator;
    private readonly ILogger<GetSavedMessagesFromDatabaseHandler> _logger;
    private readonly ILetterRepository _letterRepository;

    public GetSavedMessagesFromDatabaseHandler(
        IValidator<GetSavedMessagesFromDatabaseQuery> validator,
        ILogger<GetSavedMessagesFromDatabaseHandler> logger,
        ILetterRepository letterRepository)
    {
        _validator = validator;
        _logger = logger;
        _letterRepository = letterRepository;
    }

    public async Task<Result<List<Letter>>> Handle(
        GetSavedMessagesFromDatabaseQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();
        
        var letters = await _letterRepository.GetByCredentialsWithPagination(
            query.MailCredentialsDto.Email,
            query.Page,
            query.PageSize,
            cancellationToken);
        
        _logger.LogInformation("User {email} got messages from folder database", query.MailCredentialsDto.Email);

        return letters;
    }
}