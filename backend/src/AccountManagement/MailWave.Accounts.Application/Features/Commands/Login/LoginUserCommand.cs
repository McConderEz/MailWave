using MailWave.Core.Abstractions;

namespace MailWave.Accounts.Application.Features.Commands.Login;

public record LoginUserCommand(string Email, string Password): ICommand;