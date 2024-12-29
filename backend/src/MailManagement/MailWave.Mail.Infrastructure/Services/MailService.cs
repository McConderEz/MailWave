using CSharpFunctionalExtensions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailWave.Mail.Domain.Entities;
using MailWave.Mail.Domain.Shared;
using MailWave.Mail.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace MailWave.Mail.Infrastructure.Services;

/// <summary>
/// Сервис отправления сообщений по почте
/// </summary>
public class MailService
{
   // private readonly MailOptions _mailOptions;
    private readonly ILogger<MailService> _logger;
    private readonly EmailValidator _validator;

    public MailService(
        //IOptions<MailOptions> mailOptions,
        ILogger<MailService> logger,
        EmailValidator validator)
    {
        //_mailOptions = mailOptions.Value;
        _logger = logger;
        _validator = validator;
    }

    /// <summary>
    /// Метод отправки данных по почте
    /// </summary>
    /// <param name="letter">Письмо для отправки(адресса получателей, отправитель, основная информация)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<UnitResult<string>> Send(Letter letter, CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Execute(letter.To);
        if (validationResult.IsFailure)
            return validationResult.Error;

        letter.To = validationResult.Value;

        var mail = new MimeMessage();
        
        //TODO: Отредачить
        mail.From.Add(new MailboxAddress("minoddein.ezz@gmail.com", "minoddein.ezz@gmail.com"));

        foreach (var address in letter.To)
        {
            MailboxAddress.TryParse(address, out var mailAddress);
            mail.To.Add(mailAddress!);
        }

        var body = new BodyBuilder { HtmlBody = letter.Body };

        mail.Body = body.ToMessageBody();
        mail.Subject = letter.Subject;

        try
        {
            using var client = new SmtpClient();

            //TODO: Создать хранение настроек
            await client.ConnectAsync("", 0, cancellationToken: cancellationToken);
            await client.AuthenticateAsync("", "", cancellationToken);
            await client.SendAsync(mail, cancellationToken);

            foreach (var address in mail.To)
                _logger.LogInformation("Email successfully sended to {to}", address);

            return UnitResult.Success<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError("The email message was not sent");
            return UnitResult.Failure("The email message was not sent");
        }
    }
    
    //TODO: Добавить кэширование

    /// <summary>
    /// Получения писем из папки
    /// </summary>
    /// <param name="selectedFolder">Папка, из которой получаем</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Result со списком писем</returns>
    public async Task<Result<List<Letter>>> GetMessages(
        SpecialFolder selectedFolder, 
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new ImapClient();

            await client.ConnectAsync("", 0, cancellationToken: cancellationToken);
            await client.AuthenticateAsync("", "", cancellationToken);

            var folder = await client.GetFolderAsync(selectedFolder switch
            {
                SpecialFolder.Sent => SpecialFolder.Sent.ToString(),
                SpecialFolder.Drafts => SpecialFolder.Drafts.ToString(),
                SpecialFolder.Junk => SpecialFolder.Junk.ToString(),
                SpecialFolder.Trash => SpecialFolder.Trash.ToString(),
                _ => client.Inbox.ToString()
            }, cancellationToken);

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var result = await folder.GetMessagesAsync(page, pageSize, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Cannot receive email message");

            return Result.Failure<List<Letter>>("Cannot receive email message");
        }
    }
}