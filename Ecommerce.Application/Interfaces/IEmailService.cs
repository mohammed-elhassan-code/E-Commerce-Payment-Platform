namespace Ecommerce.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject , string htmlBody, byte[]? attachmentBytes = null, string? attachmentName = null);
    
}