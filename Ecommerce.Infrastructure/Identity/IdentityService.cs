using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.dto;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;

namespace Ecommerce.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<Customer> _passwordHasher; // أداة مايكروسوفت للتشفير الصارم 🔐

    public IdentityService(AppDbContext context , IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<Customer>();
    }

    // 1. تسجيل عميل جديد وتشفير كلمته برمجياً
    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var exsist = await _context.Customers.AnyAsync( c => c.Email.ToLower() == request.Email.ToLower());
        if(exsist) throw new ArgumentException ("البريد الالكتروني مسجل مسبقا في النظام");

        var customer =new Customer
        {
          FirstName = request.FirstName,
          LastName = request.LastName,
          Email = request.Email.ToLower()
        };
           
        // التشفير الآمن باتجاه واحد   
        customer.PasswordHash = _passwordHasher.HashPassword(customer , request.Password);

        // توليد توكن تفعيل الحساب عشوائيًا 🔑
        customer.EmailConfirmationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
          
        _context.Customers.Add(customer);
        var success =  await _context.SaveChangesAsync() > 0;

        if (success)
        {
            // محاكاة إرسال بريد إلكتروني للعميل لعام 2026 تحتوي على رابط التفعيل 📧
            string activationLink = $"http://localhost:5123/api/auth/confirm-email?token={Uri.EscapeDataString(customer.EmailConfirmationToken)}";
            Console.WriteLine($"\n[EMAIL SIMULATOR] تم إرسال رابط التفعيل لإيميل العميل:\n--> {activationLink}\n");
        }
        
        return success;

    }

    // 2. تسجيل الدخول وإصدار الـ JWT والـ Refresh Token معاً
    public async Task<TokenResponse?> LoginAsync(LoginRequest request)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync( c => c.Email.ToLower() == request.UserName.ToLower());
        if(customer is null) return null;
        
        // التحقق من صحة الكلمة المشفرة
        var result = _passwordHasher.VerifyHashedPassword(customer , customer.PasswordHash , request.Password);
        if( result == PasswordVerificationResult.Failed ) return null;
        
        // درع تفعيل الحساب الصارم: لا نُصدر توكنز لعميل لم يفعّل إيميله 🛑
        if (!customer.IsEmailConfirmed)
        {
            throw new InvalidOperationException("يرجى تفعيل حسابك أولاً عبر الرابط المرسل إلى بريدك الإلكتروني!");
        }

        return await GenerateTokenPairAsync(customer);

    }

    // 3. درع تجديد التوكن تلقائياً بدون كلمة مرور ⏳🌟
    public async Task<TokenResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _context.RefreshTokens
        .Include( rt => rt.customer)
        .FirstOrDefaultAsync( rt => rt.Token == request.RefreshToken);

        if(storedToken is null || !storedToken.IsActive )return null;

        // استهلاك التوكن الحالية (لمنع استخدامها مرتين في حال سرقتها)
        storedToken.IsUsed = true;
        _context.RefreshTokens.Update(storedToken);

        await _context.SaveChangesAsync();

        // توليد بطاقتين جديدتين تماماً للعميل
        return await GenerateTokenPairAsync(storedToken.customer);
        
    }
    
    // ميكانيكية توليد التوكنز وحفظها في قاعدة البيانات
    public async Task<TokenResponse> GenerateTokenPairAsync(Customer customer)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("Secret key is missing!");

        // توليد الـ Access Token (JWT)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new Claim(ClaimTypes.Email, customer.Email),
            new Claim(ClaimTypes.Role, customer.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2), // صالحة لساعتين
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        // توليد الـ Refresh Token العشوائية الطويلة وحفظها في السيرفر 🔑
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshTokenString = Convert.ToBase64String(randomNumber);

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            CustomerId  = customer.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new TokenResponse { AccessToken = accessToken , RefreshToken = refreshTokenString};
    }

    // تنفيذ دالة تفعيل الحساب برمجياً عند الضغط على الرابط
    public async Task<bool> ConfirmEmailAsync(string token)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.EmailConfirmationToken == token);
        if (customer is null) return false;

        customer.IsEmailConfirmed = true;
        customer.EmailConfirmationToken = null; // تنظيف الحقل لمنع إعادة الاستخدام
        
        _context.Customers.Update(customer);
        return await _context.SaveChangesAsync() > 0;
    }

    // طلب رابط استعادة كلمة المرور المنسية (Forgot Password)
    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
         var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == request.Email.ToLower());
        if (customer is null) return false; // في الشركات نُرجع false ولا نُظهر خطأ صريحًا لحماية خصوصية الحسابات

        // توليد توكن وتحديد تاريخ انتهائه بعد 30 دقيقة فقط لحماية العميل ⏳
        customer.PasswordResetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        customer.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

        _context.Customers.Update(customer);
        var success = await _context.SaveChangesAsync() > 0;

        if (success)
        {
            // محاكاة إرسال رابط إعادة التعيين الآمن 📧
            string resetLink = $"http://localhost:5019/api/auth/reset-password?token={Uri.EscapeDataString(customer.PasswordResetToken)}";
            Console.WriteLine($"\n[EMAIL SIMULATOR] تم إرسال رابط إعادة تعيين كلمة المرور:\n--> {resetLink}\n");
        }

        return success;
    }
    
    // تعيين كلمة المرور الجديدة الآمنة وتصفير حقول التوكن (Reset Password)
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
       var customer = await _context.Customers.FirstOrDefaultAsync(c => 
            c.PasswordResetToken == request.Token && 
            c.ResetTokenExpiry >= DateTime.UtcNow); // التحقق من أن التوكن لم تنتهِ صلاحيتها

        if (customer is null) throw new ArgumentException("رابط استعادة كلمة المرور غير صالح أو منتهي الصلاحية!");

        // تشفير الكلمة الجديدة وحفظها بأمان 🔐
        customer.PasswordHash = _passwordHasher.HashPassword(customer, request.NewPassword);
        
        // تصفير الحقول لمنع المخترقين من إعادة استخدام الرابط
        customer.PasswordResetToken = null;
        customer.ResetTokenExpiry = null;

        _context.Customers.Update(customer);
        return await _context.SaveChangesAsync() > 0;
    }

}