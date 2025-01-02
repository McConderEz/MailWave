using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts;

public interface IMailContract
{
    public Task<Result> CheckConnection(string userName, string password,
        CancellationToken cancellationToken = default);
}