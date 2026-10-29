namespace Ecommerce.Domain.Entities;

public class Customer
{
    public int Id {get; set;}
    public string FirstName {get; set;} = string.Empty;
    public string LastName {get; set;} = string.Empty;
    public string Email {get; set;} = string.Empty;
    public string PasswordHash {get; set;} = string.Empty; // لتخزين كلمة المرور مشفرة لاحقًا
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
}