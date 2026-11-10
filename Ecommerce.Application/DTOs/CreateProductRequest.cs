using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class CreateProductRequest
{
    [Required(ErrorMessage = "اسم المنتج مطلوب إجباريًا")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "يجب أن يكون اسم المنتج بين 2 إلى 100 حرف")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 1000000, ErrorMessage = "يجب أن يكون السعر أكبر من صفر")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "يجب تحديد القسم الذي ينتمي إليه المنتج")]
    public int CategoryId { get; set; }
    public int Stock {get; set;}
}
