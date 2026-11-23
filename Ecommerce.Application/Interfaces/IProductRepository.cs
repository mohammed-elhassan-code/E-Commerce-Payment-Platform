using Ecommerce.Domain.Entities;
using Ecommerce.Application.DTOs;
using ecommer.Application.DTOs;

namespace Ecommerce.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetFilteredAsync(ProductQueryParameters parameters);
    void Add(Product product);         
    void Update(Product product);
    void Remove(Product product);  
    Task<bool> SaveChangesAsync();     
}