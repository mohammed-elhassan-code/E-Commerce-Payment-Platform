using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Ecommerce.Application.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب للدخول")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string UserName {get; set;} = string.Empty;
    
    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    public string Password {get; set;} =string.Empty;
}