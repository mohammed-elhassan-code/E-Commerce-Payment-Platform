using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class AddToCartRequest
{
    [Required (ErrorMessage = "يجب تحديد المنتج")]
    public int ProductId {get; set;}
    [Range(1,100,ErrorMessage ="يجب أن تكون الكمية بين 1 إلى 100 قطعة")]
    public int Quantity {get; set;}
}