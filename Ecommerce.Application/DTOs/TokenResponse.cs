namespace Ecommerce.Application.DTOs;

public class TokenResponse
{
    public string AccessToken {get; set;} = string.Empty; // توكن الـ JWT قصيرة الأجل (ساعتين)
    public string RefreshToken {get; set;} = string.Empty; // توكن متجددة طويلة الأجل (7 أيام)

}