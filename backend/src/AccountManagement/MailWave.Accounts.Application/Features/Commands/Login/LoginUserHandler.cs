using FluentValidation;
using MailWave.Accounts.Application.Providers;
using MailWave.Accounts.Application.Repositories;
using MailWave.Accounts.Contracts;
using MailWave.Accounts.Contracts.Responses;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace MailWave.Accounts.Application.Features.Commands.Login;

public class LoginUserHandler: ICommandHandler<LoginUserCommand, LoginResponse>
{
    private readonly ILogger<LoginUserHandler> _logger;
    private readonly ITokenProvider _tokenProvider;
    private readonly IValidator<LoginUserCommand> _validator;
    private readonly IUserRepository _userRepository;


    public LoginUserHandler(
        ILogger<LoginUserHandler> logger,
        ITokenProvider tokenProvider,
        IValidator<LoginUserCommand> validator,
        IUserRepository userRepository)
    {
        _logger = logger;
        _tokenProvider = tokenProvider;
        _validator = validator;
        _userRepository = userRepository;
    }

    public async Task<Result<LoginResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        var validatorResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validatorResult.IsValid)
            return validatorResult.ToErrorList();

        var user = await _userRepository.GetByEmail(command.Email, cancellationToken);
        
        if (user is null)
            return Errors.General.NotFound();

        /*var passwordConfirmed = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordConfirmed)
        {
            return Errors.User.InvalidCredentials();
        }*/

        var accessToken = _tokenProvider.GenerateAccessToken(user, cancellationToken);
        var refreshToken = await _tokenProvider.GenerateRefreshToken(user, accessToken.Jti,cancellationToken);

        _logger.LogInformation("Successfully logged in");
        
        return new LoginResponse(accessToken.AccessToken, refreshToken);
    }
}