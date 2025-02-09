using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.Repositories;
using MailWave.Mail.Contracts;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Queries.GetSavedMessageById;

public class GetSavedMessageByIdHandler: IQueryHandler<Letter, GetSavedMessageByIdQuery>
{
    private readonly IValidator<GetSavedMessageByIdQuery> _validator;
    private readonly ILogger<GetSavedMessageByIdHandler> _logger;
    private readonly ILetterRepository _letterRepository;

    public GetSavedMessageByIdHandler(
        IValidator<GetSavedMessageByIdQuery> validator,
        ILogger<GetSavedMessageByIdHandler> logger,
        ILetterRepository letterRepository,
        IMailContract mailContract)
    {
        _validator = validator;
        _logger = logger;
        _letterRepository = letterRepository;
    }
    
    //TODO: Пофиксить потом
    public async Task<Result<Letter>> Handle(
        GetSavedMessageByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();
        
        var letter = await _letterRepository.GetByCredentialsAndId(
            query.MailCredentialsDto.Email,
            query.MessageId,
            cancellationToken);
        
        _logger.LogInformation("User {email} got message from folder database with messageId {id}",
            query.MailCredentialsDto.Email, query.MessageId);

        return letter;
    }
}