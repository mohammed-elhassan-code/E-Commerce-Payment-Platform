namespace Ecommerce.Application.DTOs;

public class PaymentResponse
{
    public string CheckoutUrl { get; set; } = string.Empty; // الرابط الذي سيوجه العميل لبوابة الدفع
}
