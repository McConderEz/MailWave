using FluentValidation;
using MailWave.Accounts.Application.Providers;
using MailWave.Accounts.Application.Repositories;
using MailWave.Accounts.Contracts.Messaging;
using MailWave.Accounts.Contracts.Responses;
using MailWave.Accounts.Domain.Models;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Contracts;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace MailWave.Accounts.Application.Features.Commands.Login;

/// <summary>
/// Авторизация
/// </summary>
public class LoginUserHandler: ICommandHandler<LoginUserCommand, LoginResponse>
{
    private readonly ILogger<LoginUserHandler> _logger;
    private readonly ITokenProvider _tokenProvider;
    private readonly IValidator<LoginUserCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly ICryptProvider _cryptProvider;
    private readonly IMailContract _mailContract;
    private readonly IPublishEndpoint _publishEndpoint;


    public LoginUserHandler(
        ILogger<LoginUserHandler> logger,
        ITokenProvider tokenProvider,
        IValidator<LoginUserCommand> validator,
        IUserRepository userRepository,
        ICryptProvider cryptProvider,
        IMailContract mailContract,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _tokenProvider = tokenProvider;
        _validator = validator;
        _userRepository = userRepository;
        _cryptProvider = cryptProvider;
        _mailContract = mailContract;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Метод авторизации в учётную запись. Если пользователь не существует, но при этом соединение с сервером успешно -
    /// происходит создание записи в БД. Если запись уже существует - происходит верификация захэшированного пароля.
    /// Далее создаётся токен доступа и токен обновления.
    /// Также отправляется интеграционное событие для установки учетных данных в модуль Mail
    /// </summary>
    /// <param name="command">Команда с входными параметрами</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Возвращает результат в виде ответа с токеном доступа и токеном обновления</returns>
    public async Task<Result<LoginResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        var validatorResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validatorResult.IsValid)
            return validatorResult.ToErrorList();

        var isConnected = await _mailContract
            .CheckConnection(command.Email, command.Password, cancellationToken);

        if (isConnected.IsFailure)
            return Errors.MailErrors.ConnectionError();
        
        var user = await CreateOrIdentificateUser(command, cancellationToken);

        var message = new GotUserCredentialsForMailEvent(command.Email, command.Password);

        await _publishEndpoint.Publish(message, cancellationToken);
        
        var accessToken = _tokenProvider.GenerateAccessToken(user, cancellationToken);
        var refreshToken = await _tokenProvider.GenerateRefreshToken(user, accessToken.Jti,cancellationToken);

        _logger.LogInformation("Successfully logged in");
        
        return new LoginResponse(accessToken.AccessToken, refreshToken);
    }

    /// <summary>
    /// Метод создания пользователя, если он отсутствует или его идентфикация
    /// </summary>
    /// <param name="command">Команда с входными параметрами учетной записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Объект User</returns>
    /// <exception cref="ApplicationException">Если пользователь существует,
    /// соединение успешно, но верификация пароля не проходит</exception>
    private async Task<User> CreateOrIdentificateUser(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmail(command.Email, cancellationToken);
        
        if (user is null)
        {
            var hashedPassword = _cryptProvider.HashPassword(command.Password);

            user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = command.Email,
                Password = hashedPassword
            };
            
            await _userRepository.Add(user, cancellationToken);
        }
        else
        {
            if (!_cryptProvider.Verify(user.Password, command.Password))
                throw new ApplicationException(
                    "the connection is successful, but the account information does not match");
        }

        return user;
    }
}