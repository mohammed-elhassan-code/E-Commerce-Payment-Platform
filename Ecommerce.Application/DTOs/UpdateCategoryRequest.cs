using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class UpdateCategoryRequest
{
    [Required(ErrorMessage = "اسم الفئة مطلوب إجباريًا")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "يجب أن يكون اسم الفئة بين 3 إلى 50 حرفًا")]
    public string Name { get; set; } = string.Empty;
}
