using MailWave.Core.DTOs;
using MailWave.Core.Models;
using MailWave.Framework;
using MailWave.Mail.Application.DTOs;
using MailWave.Mail.Application.Features.Commands.DeleteMessage;
using MailWave.Mail.Application.Features.Commands.MoveMessage;
using MailWave.Mail.Application.Features.Commands.SaveMessagesInDatabase;
using MailWave.Mail.Application.Features.Commands.SendMessage;
using MailWave.Mail.Application.Features.Queries.GetMessageFromFolderById;
using MailWave.Mail.Application.Features.Queries.GetMessagesFromFolderWithPagination;
using MailWave.Mail.Contracts.Requests;
using MailWave.SharedKernel.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [FromRoute] int messageId,
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
    
    [HttpPost]
    public async Task<IActionResult> SendMessage(
        [FromForm] SendMessageRequest request,
        [FromForm]IFormFileCollection? attachments,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] SendMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        List<AttachmentDto> attachmentDtos = [];
        
        if (attachments is not null)
        {
            foreach (var attachment in attachments)
            {
                attachmentDtos.Add(
                    new AttachmentDto(attachment.OpenReadStream(), attachment.FileName));
            }
        }

        var command = new SendMessageCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.Subject,
            request.Body,
            request.Receivers,
            attachmentDtos);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpPost("{messageId:int}")]
    public async Task<IActionResult> MoveMessage(
        [FromRoute] int messageId,
        [FromForm] MoveMessageRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] MoveMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new MoveMessageCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.SelectedFolder,
            request.TargetFolder,
            (uint)messageId);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpPost("{messageId:int}/deletion-message")]
    public async Task<IActionResult> DeleteMessage(
        [FromRoute] int messageId,
        [FromForm] DeleteMessageRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] DeleteMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteMessageCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.SelectedFolder,
            (uint)messageId);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpPost("/saving-message")]
    public async Task<IActionResult> SaveMessagesInDatabase(
        [FromForm] SaveMessagesToDatabaseRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] SaveMessagesInDatabaseHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new SaveMessagesInDatabaseCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.SelectedFolder,
            request.MessageIds);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
}