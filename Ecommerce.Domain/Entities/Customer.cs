using System.Security.Cryptography;

namespace Ecommerce.Domain.Entities;

public class Customer
{
    public int Id {get; set;}
    public string FirstName {get; set;} = string.Empty;
    public string LastName {get; set;} = string.Empty;
    public string Email {get; set;} = string.Empty;
    public string PasswordHash {get; set;} = string.Empty; // لتخزين كلمة المرور مشفرة آمنة 🔐
    public string Role {get; set;} = "customer";  // تحديد الدور (Admin أو Customer)
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

    // --- حقول أمان تفعيل الحساب عبر الإيميل 📧 ---
    public bool IsEmailConfirmed {get; set;} = false; // هل العميل ضغط على رابط التفعيل؟
    public string? EmailConfirmationToken {get; set;} // الرمز المشفر المرفق بالرابط

    public string? PasswordResetToken {get; set;} // الرمز المشفر لرابط الاستعادة 
    public DateTime? ResetTokenExpiry {get; set;} // تاريخ انتهاء الرابط (مثلاً صالح لنصف ساعة فقط)


    // علاقة واحد لمتعدد: العميل يمكن أن يمتلك أكثر من Refresh Token عبر أجهزته المختلفة
    public ICollection<RefreshToken> RefreshTokens = new List<RefreshToken>();
}