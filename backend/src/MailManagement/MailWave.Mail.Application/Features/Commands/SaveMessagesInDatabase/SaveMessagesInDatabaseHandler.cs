using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Application.Repositories;
using MailWave.Mail.Contracts;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
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
    private readonly IMailContract _mailContract;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILetterRepository _repository;

    public SaveMessagesInDatabaseHandler(
        IValidator<SaveMessagesInDatabaseCommand> validator,
        ILogger<SaveMessagesInDatabaseHandler> logger,
        IMailService mailService,
        IMailContract mailContract, 
        IUnitOfWork unitOfWork,
        ILetterRepository repository)
    {
        _validator = validator;
        _logger = logger;
        _mailService = mailService;
        _mailContract = mailContract;
        _unitOfWork = unitOfWork;
        _repository = repository;
    }

    /// <summary>
    /// Обработчик
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<Result> Handle(
        SaveMessagesInDatabaseCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();

        var transaction = await _unitOfWork.BeginTransaction(cancellationToken);
        try
        {
            var message = await _mailService.GetMessage(
                command.MailCredentialsDto,
                command.EmailFolder,
                command.MessageIds.FirstOrDefault(),
                cancellationToken);

            if (message.IsFailure)
                return message.Errors;

            if (message.Value.IsCrypted)
            {
                var result = await _mailContract.GetDecryptedLetter(
                    command.MailCredentialsDto,
                    command.EmailFolder,
                    message.Value.Id,
                    cancellationToken);

                if(result.IsFailure)
                    return result.Errors;

                message.Value.Body = result.Value.Body;
            }

            var isExist = await _repository
                .GetById(message.Value.Folder, message.Value.Id, message.Value.EmailPrefix, cancellationToken);

            if (isExist.IsFailure)
                await _repository.Add([message.Value], cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            transaction.Commit();

            _logger.LogInformation("batch of letters was saved in database");

            return Result.Success();
        }
        catch (Exception ex)
        {
            transaction.Rollback();

            _logger.LogError("Fail to save letters to database.Ex. message: {ex} ", ex.Message);

            return Error.Failure("save.db.fail", "Cannot save letters in db");
        }
    }
}