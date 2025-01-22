using MailWave.Core.Abstractions;

namespace MailWave.Accounts.Application.Features.Commands.Refresh;

public record RefreshTokenCommand(string AccessToken, Guid RefreshToken) : ICommand;