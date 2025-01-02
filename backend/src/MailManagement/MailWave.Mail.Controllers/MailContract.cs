using MailWave.Mail.Contracts;
using MailWave.Mail.Infrastructure.Services;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Controllers;

public class MailContract: IMailContract
{
    private readonly MailService _mailService;

    public MailContract(MailService mailService)
    {
        _mailService = mailService;
    }
    
    public async Task<Result> CheckConnection(
        string userName, string password, CancellationToken cancellationToken = default)
    {
        var result = await _mailService.CheckConnection(userName, password, cancellationToken);
        if (result.IsFailure)
            return result.Errors;

        return Result.Success();
    }
}