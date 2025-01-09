using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.SaveMessagesInDatabase;

/// <summary>
/// Сохранение писем из почты в локальную базу данных для долгосрочного хранения
/// </summary>
public class SaveMessagesInDatabaseHandler: ICommandHandler<SaveMessagesInDatabaseCommand>
{
    private readonly IValidator<SaveMessagesInDatabaseCommand> _validator;
    private readonly ILogger<SaveMessagesInDatabaseHandler> _logger;
    private readonly IMailService _mailService;

    public SaveMessagesInDatabaseHandler(
        IValidator<SaveMessagesInDatabaseCommand> validator,
        ILogger<SaveMessagesInDatabaseHandler> logger,
        IMailService mailService)
    {
        _validator = validator;
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// Обработчик
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> Handle(SaveMessagesInDatabaseCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var result = await _mailService.SaveMessagesInDataBase(command.MailCredentialsDto, command.MessageIds,
            command.EmailFolder, cancellationToken);

        if (result.IsFailure)
            return result.Errors;
        
        _logger.LogInformation("User saved letters in database");
        
        return Result.Success();
    }
}