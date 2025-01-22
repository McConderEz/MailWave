using FluentValidation;
using MailWave.Accounts.Application.Managers;
using MailWave.Accounts.Application.Providers;
using MailWave.Accounts.Contracts.Responses;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Core.Models;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;

namespace MailWave.Accounts.Application.Features.Commands.Refresh;

public class RefreshTokenHandler: ICommandHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IRefreshSessionManager _refreshSessionManager;
    private readonly ITokenProvider _tokenProvider;
    private readonly IValidator<RefreshTokenCommand> _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenHandler(
        IRefreshSessionManager refreshSessionManager,
        IValidator<RefreshTokenCommand> validator,
        IDateTimeProvider dateTimeProvider,
        ITokenProvider tokenProvider)
    {
        _refreshSessionManager = refreshSessionManager;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
        _tokenProvider = tokenProvider;
    }
    
    public async Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var validatorResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validatorResult.IsValid)
            return validatorResult.ToErrorList();

        var refreshSession = await _refreshSessionManager
            .GetByRefreshToken(command.RefreshToken, cancellationToken);
        
        if (refreshSession.IsFailure)
            return refreshSession.Errors;

        if (refreshSession.Value.ExpiresIn < _dateTimeProvider.UtcNow)
            return Errors.Tokens.ExpiredToken();
        
        await _refreshSessionManager.Delete(refreshSession.Value, cancellationToken);
        
        var accessToken = _tokenProvider
            .GenerateAccessToken(refreshSession.Value.User,cancellationToken);
        var refreshToken = await _tokenProvider
            .GenerateRefreshToken(refreshSession.Value.User,accessToken.Jti, cancellationToken);

        return new LoginResponse(accessToken.AccessToken, refreshToken);
    }
}