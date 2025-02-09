using MailWave.Core.Abstractions;
using MailWave.Core.DTOs;

namespace MailWave.Mail.Application.Features.Queries.GetSavedMessagesFromDatabase;

public record GetSavedMessagesFromDatabaseQuery(MailCredentialsDto MailCredentialsDto, int Page, int PageSize) : IQuery;
