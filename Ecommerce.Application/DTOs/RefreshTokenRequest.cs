using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken {get; set;} = string.Empty; // يرسله العميل عندما تنتهي صلاحية الـ JWT 
}