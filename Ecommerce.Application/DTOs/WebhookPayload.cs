namespace Ecommerce.Application.DTOs;

public class WebhookPayload
{
    public string EventType { get; set; } = string.Empty;          // نوع الحدث مثل payment.succeeded
    public int OrderId { get; set; }                               // رقم الطلب المرتبط بالدفع
    public string TransactionId { get; set; } = string.Empty;      // رقم المعاملة الفريد من البنك
    public string Signature { get; set; } = string.Empty;          // التوقيع المشفر للتحقق من أصل الإشارة 🛡️
}
