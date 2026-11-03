using System.Security.Cryptography;

namespace Ecommerce.Domain.Entities;

public class RefreshToken
{
    public int Id {get; set;}
    public string Token {get; set;} =string.Empty;
    public DateTime ExpiresAt {get; set;}
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsUsed {get; set;}
    public bool IsRevoked {get; set;} // هل تم الغاء التوكن من قبل المشرفين
    public bool IsActive => !IsUsed && !IsRevoked && !IsExpired;

    // ربط التوكن بالعميل صاحب الحساب
    public int CustomerId {get; set;}
    public Customer customer {get; set;} = null!;

}