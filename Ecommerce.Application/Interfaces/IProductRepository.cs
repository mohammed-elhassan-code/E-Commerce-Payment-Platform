using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    void Update(Product product); // دالة التحديث لحفظ كميات المخزن الجديدة 📦
}