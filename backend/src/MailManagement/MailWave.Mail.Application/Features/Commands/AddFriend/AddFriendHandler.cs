using FluentValidation;
using MailWave.Core.Abstractions;
using MailWave.Core.Extensions;
using MailWave.Mail.Application.CryptProviders;
using MailWave.Mail.Application.MailService;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Application.Features.Commands.AddFriend;

public class AddFriendHandler: ICommandHandler<AddFriendCommand>
{
    private readonly ILogger<AddFriendHandler> _logger;
    private readonly IValidator<AddFriendCommand> _validator;
    private readonly IMailService _mailService;
    private readonly IRsaCryptProvider _rsaCryptProvider;

    public AddFriendHandler(
        ILogger<AddFriendHandler> logger,
        IValidator<AddFriendCommand> validator,
        IMailService mailService,
        IRsaCryptProvider rsaCryptProvider)
    {
        _logger = logger;
        _validator = validator;
        _mailService = mailService;
        _rsaCryptProvider = rsaCryptProvider;
    }

    public async Task<Result> Handle(AddFriendCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrorList();
        
        await _mailService.SendMessage(command.MailCredentialsDto, null, new Letter(), cancellationToken);
        
        //TODO: Реализовать
        
        return Result.Success();
    }
}