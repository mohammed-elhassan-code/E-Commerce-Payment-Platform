using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string Code);
}