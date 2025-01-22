using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MailWave.Accounts.Application.Managers;
using MailWave.Accounts.Application.Models;
using MailWave.Accounts.Application.Providers;
using MailWave.Accounts.Domain.Models;
using MailWave.Core.Models;
using MailWave.Core.Options;
using MailWave.Framework;
using MailWave.SharedKernel.Shared;
using MailWave.SharedKernel.Shared.Errors;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MailWave.Accounts.Infrastructure.Providers;

public class JwtTokenProvider: ITokenProvider
{
    //TODO: потом пофиксить options, но сейчас нет времени
    public const int EXPIRED_DAYS_TIME = 30;
    
    private readonly RefreshSessionOptions _refreshSessionOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRefreshSessionManager _refreshSessionManager;
    

    public JwtTokenProvider(
        IOptions<RefreshSessionOptions> refreshSessionOptions,
        IOptions<JwtOptions> jwtOptions,
        IDateTimeProvider dateTimeProvider,
        IRefreshSessionManager refreshSessionManager)
    {
        _refreshSessionOptions = refreshSessionOptions.Value;
        _jwtOptions = jwtOptions.Value;
        _dateTimeProvider = dateTimeProvider;
        _refreshSessionManager = refreshSessionManager;
    }

    public JwtTokenResult GenerateAccessToken(User user, CancellationToken cancellationToken = default)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var jti = Guid.NewGuid();
        
        var claims = new[]
        {
            new Claim(CustomClaims.Id, user.Id.ToString()),
            new Claim(CustomClaims.Email, user.Email!),
            new Claim(CustomClaims.Jti, jti.ToString())
        };
        
        var jwtToken = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_jwtOptions.ExpiredMinutesTime)),
            signingCredentials: signingCredentials,
            claims: claims);


        var jwtStringToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        return new JwtTokenResult(jwtStringToken, jti);
    }

    public async Task<Guid> GenerateRefreshToken(
        User user,
        Guid accessTokenJti,
        CancellationToken cancellationToken = default)
    {
        var refreshSession = new RefreshSession()
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            User = user,
            CreatedAt = _dateTimeProvider.UtcNow,
            Jti = accessTokenJti.ToString(),
            ExpiresIn = _dateTimeProvider.UtcNow.AddDays(30),
            RefreshToken = Guid.NewGuid().ToString()
        };

        await _refreshSessionManager.Add(refreshSession, cancellationToken);

        return Guid.Parse(refreshSession.RefreshToken);
    }
    
    public async Task<Result<IReadOnlyList<Claim>>> GetUserClaimsFromJwtToken(
        string jwtToken,
        CancellationToken cancellationToken = default)
    {
        var jwtHandler = new JwtSecurityTokenHandler();

        var validationParameters = TokenValidationParametersFactory.CreateWithoutLifeTime(_jwtOptions);

        var validationResult = await jwtHandler.ValidateTokenAsync(jwtToken, validationParameters);
        if (!validationResult.IsValid)
            return Errors.Tokens.InvalidToken();

        return validationResult.ClaimsIdentity.Claims.ToList();
    }
}