using MailWave.Core.DTOs;
using MailWave.Mail.Application.Features.Queries.GetCryptedMessageFromFolderById;
using MailWave.Mail.Contracts;
using MailWave.Mail.Infrastructure.Services;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Controllers;

public class MailContract: IMailContract
{
    private readonly MailService _mailService;
    private readonly GetCryptedMessageFromFolderByIdHandler _getCryptedMessageFromFolderByIdHandler;
    
    public MailContract(
        MailService mailService,
        GetCryptedMessageFromFolderByIdHandler getCryptedMessageFromFolderByIdHandler)
    {
        _mailService = mailService;
        _getCryptedMessageFromFolderByIdHandler = getCryptedMessageFromFolderByIdHandler;
    }
    
    public async Task<Result> CheckConnection(
        string userName, string password, CancellationToken cancellationToken = default)
    {
        var result = await _mailService.CheckConnection(userName, password, cancellationToken);
        if (result.IsFailure)
            return result.Errors;

        return Result.Success();
    }

    public async Task<Result<string>> GetDecryptedBody(
        MailCredentialsDto mailCredentialsDto,
        Constraints.EmailFolder emailFolder,
        uint messageId,
        CancellationToken cancellationToken = default)
    {
        var result = await _getCryptedMessageFromFolderByIdHandler.Handle(
            new GetCryptedMessageFromFolderByIdQuery(mailCredentialsDto, emailFolder, messageId), cancellationToken);

        if (result.IsFailure || string.IsNullOrWhiteSpace(result.Value.Body))
            return result.Errors;

        return result.Value.Body;
    }
}