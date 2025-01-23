using MailWave.Core.Abstractions;

namespace MailWave.Accounts.Application.Features.Commands.DeleteRefreshSession;

public record DeleteRefreshTokenCommand(Guid RefreshToken) : ICommand;
