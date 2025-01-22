using MailWave.Core.Abstractions;

namespace MailWave.Accounts.Application.Features.Commands.Refresh;

public record RefreshTokenCommand(Guid RefreshToken) : ICommand;