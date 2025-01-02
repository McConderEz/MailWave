using MailWave.SharedKernel.Shared;

namespace MailWave.Core.Common;

public class DateTimeProvider: IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}