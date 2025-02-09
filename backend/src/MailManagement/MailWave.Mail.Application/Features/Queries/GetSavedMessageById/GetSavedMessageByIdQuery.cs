using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;

namespace MailWave.Mail.Application.Features.Queries.GetSavedMessageById;

public record GetSavedMessageByIdQuery(MailCredentialsDto MailCredentialsDto, uint MessageId) : IQuery;
