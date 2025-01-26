using MailWave.Core.DTOs;
using MailWave.Core.Models;
using MailWave.Framework;
using MailWave.Mail.Application.DTOs;
using MailWave.Mail.Application.Features.Commands.AcceptFriendship;
using MailWave.Mail.Application.Features.Commands.AddFriend;
using MailWave.Mail.Application.Features.Commands.DeleteFriend;
using MailWave.Mail.Application.Features.Commands.DeleteMessage;
using MailWave.Mail.Application.Features.Commands.MoveMessage;
using MailWave.Mail.Application.Features.Commands.SaveFiles;
using MailWave.Mail.Application.Features.Commands.SaveMessagesInDatabase;
using MailWave.Mail.Application.Features.Commands.SendCryptOrSignedMessage;
using MailWave.Mail.Application.Features.Commands.SendMessage;
using MailWave.Mail.Application.Features.Commands.SendScheduledMessage;
using MailWave.Mail.Application.Features.Commands.VerifyMessage;
using MailWave.Mail.Application.Features.Queries.GetCryptedMessageFromFolderById;
using MailWave.Mail.Application.Features.Queries.GetMessageFromFolderById;
using MailWave.Mail.Application.Features.Queries.GetMessagesCountFromFolder;
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

        return Ok(result.Value);
    }
    
    [HttpGet("messages-count")]
    public async Task<IActionResult> GetMessagesCountFromFolder(
        [FromQuery] GetMessagesCountFromFolderRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] GetMessagesCountFromFolderHandler handler,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessagesCountFromFolderQuery(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password), request.SelectedFolder);

        var result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result.Value);
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
    
    [HttpGet("{messageId:int}/crypted-signed-message-from-folder-by-id")]
    public async Task<IActionResult> GetCryptedAndSignedMessageFromFolderById(
        [FromRoute] int messageId,
        [FromQuery] GetCryptedAndSignedMessageFromFolderByIdRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] GetCryptedMessageFromFolderByIdHandler handler,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCryptedMessageFromFolderByIdQuery(
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
        var attachmentDtos = LoadAttachments(attachments);

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
    
    [HttpPost("verification-message")]
    public async Task<IActionResult> VerifyMessage(
        [FromForm] VerifyMessageRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] VerifyMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new VerifyMessageCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.EmailFolder,
            request.MessageId);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpPost("saving-files")]
    public async Task<IActionResult> SaveFiles(
        [FromForm] SaveFilesRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] SaveFilesHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new SaveFilesCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.SelectedFolder,
            request.DirectoryPath,
            request.MessageId);

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
    
    [HttpPost("saving-message")]
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
    
    [HttpPost("scheduled-message")]
    public async Task<IActionResult> SendScheduledMessage(
        [FromForm] SendScheduledMessageRequest request,
        [FromForm] IFormFileCollection? attachments,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] SendScheduledMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        var attachmentDtos = LoadAttachments(attachments);

        var command = new SendScheduledMessageCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.Subject,
            request.Body,
            request.EnqueueAt,
            request.Receivers,
            attachmentDtos);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpPost("sending-friend-request")]
    public async Task<IActionResult> SendFriendRequest(
        [FromBody] AddFriendRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] AddFriendHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new AddFriendCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password), request.Receiver);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpPost("accepting-friend-request")]
    public async Task<IActionResult> AcceptFriendship(
        [FromForm] AcceptFriendRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] AcceptFriendshipHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new AcceptFriendshipCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.EmailFolder,
            request.MessageId);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpPost("sending-crypted-and-signed-message")]
    public async Task<IActionResult> SendCryptedAndSignedMessage(
        [FromForm] SendCryptedAndSignedMessageRequest request,
        [FromForm] IFormFileCollection? attachments,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] SendCryptOrSignedMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        var attachmentDtos = LoadAttachments(attachments);

        var command = new SendCryptOrSignedMessageCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password),
            request.IsCrypted,
            request.IsSigned,
            request.Subject,
            request.Body,
            request.Receivers,
            attachmentDtos);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }
    
    [HttpPost("deletion-friend")]
    public async Task<IActionResult> DeleteFriend(
        [FromForm] DeleteFriendRequest request,
        [FromServices] MailCredentialsScopedData mailCredentials,
        [FromServices] DeleteFriendHandler handler,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteFriendCommand(
            new MailCredentialsDto(mailCredentials.Email, mailCredentials.Password), request.FriendEmail);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            result.Errors.ToResponse();

        return Ok(result);
    }

    private List<AttachmentDto> LoadAttachments(IFormFileCollection? attachments)
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

        return attachmentDtos;
    }
}