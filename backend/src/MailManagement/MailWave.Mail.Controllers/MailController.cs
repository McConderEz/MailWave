using MailWave.Core.DTOs;
using MailWave.Core.Models;
using MailWave.Framework;
using MailWave.Mail.Application.Features.Queries.GetMessageFromFolderById;
using MailWave.Mail.Application.Features.Queries.GetMessagesFromFolderWithPagination;
using MailWave.Mail.Contracts.Requests;
using MailWave.SharedKernel.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailWave.Mail.Controllers;

[Authorize]
public class MailController: ApplicationController
{
    [HttpGet(Name = "GetMessagesFromFolderWithPagination")]
    public async Task<IActionResult> GetMessagesFromFolder(
        [FromQuery] GetMessagesFromFolderWithPaginationRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] GetMessagesFromFolderWithPaginationHandler handler,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessagesFromFolderWithPaginationQuery(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.EmailFolder,
            request.Page,
            request.PageSize);

        var result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpGet("{messageId:int}/message-from-folder-by-id")]
    public async Task<IActionResult> GetMessageFromFolderById(
        int messageId,
        [FromQuery] GetMessageFromFolderByIdRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] GetMessageFromFolderByIdHandler handler,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessageFromFolderByIdQuery(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.EmailFolder,
            (uint)messageId);

        var result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
}