using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RRealEstateApi.Services.Implementations;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var port = int.Parse(_configuration["EmailSettings:Port"]);
        var senderName = _configuration["EmailSettings:SenderName"];
        var senderEmail = _configuration["EmailSettings:SenderEmail"];
        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];

        var mail = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);

        using (var smtp = new SmtpClient(smtpServer, port))
        {
            smtp.Credentials = new NetworkCredential(username, password);
            smtp.EnableSsl = true;

            await smtp.SendMailAsync(mail);
        }
    }
}