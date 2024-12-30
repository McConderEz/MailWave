using MailWave.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MailWave.Framework;

[ApiController]
[Route("api/[controller]")]
public abstract class ApplicationController: ControllerBase
{
    public override OkObjectResult Ok(object? value)
    {
        var envelope = Envelope.Ok(value);

        return base.Ok(envelope);
    }
    
    
}