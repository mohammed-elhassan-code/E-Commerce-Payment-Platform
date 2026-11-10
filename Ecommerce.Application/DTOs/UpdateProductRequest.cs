using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class UpdateProductRequest
{
    [Required(ErrorMessage = "اسم المنتج مطلوب للتعديل")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 1000000, ErrorMessage = "يجب أن يكون السعر أكبر من صفر")]
    public decimal Price { get; set; }

    public int CategoryId {get; set;}
    
    public int Stock { get; set; }
}
