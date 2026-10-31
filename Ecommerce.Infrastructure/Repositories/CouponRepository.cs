using System.Text.Json;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Ecommerce.Infrastructure.Repositories;

public class CouponRepository:ICouponRepository
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cach; // حقن واجهة الـ Cache الذكية ⚡
    public CouponRepository(AppDbContext context , IDistributedCache cache)
    {
        _context = context;
        _cach = cache;
    }
    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        string cacheKey = $"coupon-{code.ToUpper()}";
        
        // 1. محاولة جلب الكوبون من الـ Cache السريع (RAM)
        var cachedCoupon = await _cach.GetStringAsync(cacheKey);
        if(!string.IsNullOrEmpty(cachedCoupon))
        {
            // إذا وجده، نقوم بتحويل النص (JSON) إلى كائن كود C# فوراً ونرجعه للعميل
            return JsonSerializer.Deserialize<Coupon>(cachedCoupon);
        }

        // 2. إذا لم يجده في الـ Cache (Cache Miss)، يذهب لقاعدة البيانات الحقيقية SQL Server
         var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());
        
        // 3. إذا كان الكوبون موجوداً في الـ DB، نحفظه في الـ Cache ليخدم العملاء القادمين بسرعة
         if(coupon != null)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // يبقى في الـ Cache لمدة 10 دقائق فقط لحمايته ⏳
            };

            var serializedCoupon = JsonSerializer.Serialize(coupon);
            await _cach.SetStringAsync(cacheKey , serializedCoupon , cacheOptions);

        }
        return coupon;
    }
}