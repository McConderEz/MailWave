using CSharpFunctionalExtensions;
using MailKit.Net.Smtp;
using MailWave.Mail.Domain.Entities;
using MailWave.Mail.Domain.Shared;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace MailWave.Mail.Infrastructure.Services;

/// <summary>
/// Сервис отправления сообщений по почте
/// </summary>
public class MailSenderService
{
   // private readonly MailOptions _mailOptions;
    private readonly ILogger<MailSenderService> _logger;
    private readonly EmailValidator _validator;

    public MailSenderService(
        //IOptions<MailOptions> mailOptions,
        ILogger<MailSenderService> logger,
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
    /// <returns></returns>
    public async Task<UnitResult<string>> Send(Letter letter)
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

        using var client = new SmtpClient();

        //TODO: Создать хранение настроек
        //await client.ConnectAsync(_mailOptions.Host, _mailOptions.Port);
        //await client.AuthenticateAsync(_mailOptions.UserName, _mailOptions.Password);
        await client.SendAsync(mail);

        foreach (var address in mail.To)
            _logger.LogInformation("Email successfully sended to {to}", address);

        return UnitResult.Success<string>();
    }
}