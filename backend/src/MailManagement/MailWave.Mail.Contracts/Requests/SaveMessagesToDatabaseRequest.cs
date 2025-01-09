using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Contracts.Requests;

public record SaveMessagesToDatabaseRequest(Constraints.EmailFolder SelectedFolder, IEnumerable<uint> MessageIds);
