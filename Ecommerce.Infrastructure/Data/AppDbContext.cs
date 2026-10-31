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
    public DbSet<Coupon> Coupons {get; set;}

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
            
            modelBuilder.Entity<Coupon>()
            .Property(c => c.DiscountPercentage)
            .HasPrecision(5 , 2);

            // --- حقن بيانات تجريبية حية (Seed Data) لعام 2026 ---
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "الالكترونيات" }
            );


            modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "iPhone 15 Pro", Price = 1200.00m, Stock = 5, CategoryId = 1 }, // مخزون حرج (5 قطع فقط) 📦
            new Product { Id = 2, Name = "AirPods Pro", Price = 250.00m, Stock = 50, CategoryId = 1 }
        );

        modelBuilder.Entity<Coupon>().HasData(
            new Coupon { Id = 1, Code = "SAVE20", DiscountPercentage = 20.00m, ExpiryDate = new DateTime(2026, 12, 31), IsActive = true }, // كوبون فعال صالح حتى نهاية 2026 ⏳
            new Coupon { Id = 2, Code = "EXPIRED50", DiscountPercentage = 50.00m, ExpiryDate = new DateTime(2025, 01, 01), IsActive = true } // كوبون منتهي الصلاحية للفحص 🚫
        );


    }
}
