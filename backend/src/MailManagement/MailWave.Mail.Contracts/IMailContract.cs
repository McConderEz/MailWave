using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts;

public interface IMailContract
{
    public Task<Result> CheckConnection(string userName, string password,
        CancellationToken cancellationToken = default);

    public Task<Result<string>> GetDecryptedBody(
        MailCredentialsDto mailCredentialsDto,
        Constraints.EmailFolder emailFolder,
        uint messageId,
        CancellationToken cancellationToken = default);
}