using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class ResetPasswordRequest
{
    [Required]
    public string Token { get; set; } = string.Empty; // التوكن القادم من رابط الإيميل

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "يجب أن تكون كلمة المرور 6 أحرف على الأقل")]
    public string NewPassword { get; set; } = string.Empty;
}
