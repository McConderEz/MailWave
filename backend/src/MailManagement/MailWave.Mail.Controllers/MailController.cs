﻿using MailWave.Core.DTOs;
using MailWave.Core.Models;
using MailWave.Framework;
using MailWave.Mail.Application.Features.Queries.GetMessagesFromFolderWithPagination;
using MailWave.Mail.Contracts.Requests;
using MailWave.Mail.Domain.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailWave.Mail.Controllers;

[Authorize]
public class MailController: ApplicationController
{
    [HttpGet]
    public async Task<IActionResult> GetMessagesFromFolder(
        [FromQuery] GetMessagesFromFolderWithPaginationRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] GetMessagesFromFolderWithPaginationHandler handler,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessagesFromFolderWithPaginationQuery(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            Constraints.EmailFolder.Inbox,
            request.Page,
            request.PageSize);

        var result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
}