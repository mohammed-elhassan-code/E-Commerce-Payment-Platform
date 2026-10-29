using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Customer> Customers {get; set;}
    public DbSet<CartItem> CartItems {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ضبط دقة حقل السعر في قاعدة البيانات
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        // إعداد العلاقة الصارمة بين المنتج والفئة (Non-Nullable & Restrict) 🛑
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // إعدادات جدول العملاء: جعل البريد الإلكتروني فريدًا (Unique) لمنع تكرار الحسابات 🛡️
            modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        // إعدادات جدول عناصر السلة
            modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Cascade); // إذا حُذف المنتج تُحذف عناصر السلة المرتبطة به تلقائيًا
            

    }
}
