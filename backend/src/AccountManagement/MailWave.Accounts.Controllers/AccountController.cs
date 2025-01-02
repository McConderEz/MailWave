using MailWave.Accounts.Application.Features.Commands.Login;
using MailWave.Accounts.Contracts.Requests;
using MailWave.Framework;
using Microsoft.AspNetCore.Mvc;

namespace MailWave.Accounts.Controllers;

public class AccountController : ApplicationController
{
    [HttpPost("authentication")]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserRequest request, 
        [FromServices] LoginUserHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new LoginUserCommand(request.Email, request.Password);

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
            return result.Errors.ToResponse();
        
        return Ok(result.Value);
    }
}