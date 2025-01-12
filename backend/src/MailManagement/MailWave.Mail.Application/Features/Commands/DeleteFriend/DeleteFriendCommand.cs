using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;

namespace MailWave.Mail.Application.Features.Commands.DeleteFriend;

public record DeleteFriendCommand(MailCredentialsDto MailCredentialsDto, string FriendEmail) : ICommand;
