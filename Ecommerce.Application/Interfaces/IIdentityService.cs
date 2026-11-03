using Ecommerce.Application.dto;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces;

public interface IIdentityService
{
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<TokenResponse?> LoginAsync(LoginRequest request);
    Task<TokenResponse?> RefreshTokenAsync(RefreshTokenRequest request);

    Task<bool> ConfirmEmailAsync(string token);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}