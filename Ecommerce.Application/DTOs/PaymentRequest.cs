using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class PaymentRequest
{
    [Required(ErrorMessage = "يجب تحديد رقم الطلب لإتمام عملية الدفع")]
    public int OrderId { get; set; }
}
