
using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;

namespace MailWave.Mail.Application.Features.Commands.AddFriend;

public record AddFriendCommand(MailCredentialsDto MailCredentialsDto, string Receiver) : ICommand;