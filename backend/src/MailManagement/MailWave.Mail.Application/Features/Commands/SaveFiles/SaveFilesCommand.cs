using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;
using MailWave.SharedKernel.Shared;

namespace MailWave.Mail.Application.Features.Commands.SaveFiles;

public record SaveFilesCommand(
    MailCredentialsDto MailCredentialsDto,
    Constraints.EmailFolder EmailFolder,
    string DirectoryPath,
    string FileName,
    uint MessageId) : ICommand;
