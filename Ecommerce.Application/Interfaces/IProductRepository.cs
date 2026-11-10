using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<List<Product>> GetAllAsync(); 
    void Add(Product product);         
    void Update(Product product);
    void Remove(Product product);  
    Task<bool> SaveChangesAsync();     
}