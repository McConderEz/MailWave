using MailWave.Accounts.Application.Features.Commands.Login;
using MailWave.Accounts.Application.Features.Commands.Refresh;
using MailWave.Accounts.Contracts.Requests;
using MailWave.Framework;
using Microsoft.AspNetCore.Mvc;

namespace MailWave.Accounts.Controllers;

public class AccountController : ApplicationController
{
    [HttpPost("authentification")]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserRequest request, 
        [FromServices] LoginUserHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new LoginUserCommand(request.Email, request.Password);

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
            return result.Errors.ToResponse();
        
        HttpContext.Response.Cookies.Append("refreshToken", result.Value.RefreshToken.ToString());
        
        return Ok(result.Value);
    }
    
    [HttpPost("refreshing")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        [FromServices] RefreshTokenHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
            return result.Errors.ToResponse();

        return Ok(result.Value);
    }
}