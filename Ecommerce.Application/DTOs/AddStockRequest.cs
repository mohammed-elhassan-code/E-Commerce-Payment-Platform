using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;
public class AddStockERquest
{
    [Range(1, 100000, ErrorMessage = "يجب أن تكون الكمية المضافة أكبر من صفر")]
    public int QuantityToAdd {get; set;}
}