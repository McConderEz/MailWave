using FluentValidation;
using MailWave.Accounts.Application.Managers;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Accounts.Application.Features.Commands.DeleteRefreshSession;

public class DeleteRefreshTokenHandler: ICommandHandler<DeleteRefreshTokenCommand>
{
    private readonly ILogger<DeleteRefreshTokenHandler> _logger;
    private readonly IValidator<DeleteRefreshTokenCommand> _validator;
    private readonly IRefreshSessionManager _refreshSessionManager;

    public DeleteRefreshTokenHandler(
        ILogger<DeleteRefreshTokenHandler> logger,
        IValidator<DeleteRefreshTokenCommand> validator,
        IRefreshSessionManager refreshSessionManager)
    {
        _logger = logger;
        _validator = validator;
        _refreshSessionManager = refreshSessionManager;
    }

    public async Task<Result> Handle(
        DeleteRefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var validatorResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validatorResult.IsValid)
            return validatorResult.ToErrorList();

        var refreshSession = await _refreshSessionManager
            .GetByRefreshToken(command.RefreshToken, cancellationToken);
        
        if (refreshSession.IsFailure)
            return refreshSession.Errors;
        
        await _refreshSessionManager.Delete(refreshSession.Value, cancellationToken);

        _logger.LogInformation("RefreshSession has been deleted");
        
        return Result.Success();
    }
}