using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.dto;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email {get; set;} = string.Empty;
}