namespace MailWave.SharedKernel.Shared;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}