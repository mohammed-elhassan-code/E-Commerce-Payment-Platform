using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "الاسم الأول مطلوب")]
    public string FirstName {get; set;} = string.Empty;
    [Required(ErrorMessage = "الاسم الاخير مطلوب")]
    public string LastName {get; set;} = string.Empty;
    [Required(ErrorMessage = "البريد الالكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email {get; set;} = string.Empty;
     [Required(ErrorMessage = "كلمة المرور مطلوية")]
     [StringLength(100 , MinimumLength = 6 , ErrorMessage = "يجب أن تكون كلمة المرور 6 أحرف على الأقل")]
    public string Password {get; set;} =string.Empty;
}