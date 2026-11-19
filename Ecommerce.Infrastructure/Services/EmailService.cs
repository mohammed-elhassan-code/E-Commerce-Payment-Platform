using Ecommerce.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Utils;

namespace Ecommerce.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentName = null)
    {
        var email = new MimeMessage();

        // تحديد المرسل والمستقبل
        var senderName = _configuration["EmailSettings:SenderName"];
        var senderEmail = _configuration["EmailSettings:SenderEmail"];
        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

         // إضافة معرف فريد للرسالة وتاريخ الإرسال لتجنب فلاتر السبام
        email.MessageId = MimeUtils.GenerateMessageId();
        email.Date = DateTimeOffset.UtcNow;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
        };

         // إضافة ملف الـ PDF كمرفق إذا تم تمريره
        if (attachmentBytes != null && !string.IsNullOrEmpty(attachmentName))
        {
        builder.Attachments.Add(attachmentName, attachmentBytes, new ContentType("application", "pdf"));
        }

        email.Body = builder.ToMessageBody();

        // الاتصال بخادم SMTP والإرسال الآمن
        using var smtp = new SmtpClient();
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
      

        var portString = _configuration["EmailSettings:Port"];
        int port = int.TryParse(portString, out int parsedPort) ? parsedPort : 587;

        var password = _configuration["EmailSettings:Password"];

        await smtp.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(senderEmail, password);
        var sendRequest = new MailKit.SendRequest(email);
        await smtp.SendAsync(sendRequest);
        await smtp.DisconnectAsync(true);
    }
}
