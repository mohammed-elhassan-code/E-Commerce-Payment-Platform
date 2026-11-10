using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Services;

public class ProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if(product is null) return null;

        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        };

    }
    public async Task<List<ProductResponse>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(p => new ProductResponse { Id = p.Id, Name = p.Name, Price = p.Price }).ToList();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var product = new Product { Name = request.Name, Price = request.Price, CategoryId = request.CategoryId, Stock = request.Stock};
        _productRepository.Add(product);
        await _productRepository.SaveChangesAsync();
        return new ProductResponse { Id = product.Id, Name = product.Name, Price = product.Price };
    }

    public async Task<ProductResponse?> UpdateAsync(int id , UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if(product is null)
        {
            return null;
        }

        product.Name = request.Name;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        
        await _productRepository.SaveChangesAsync();

        return new ProductResponse { Id = product.Id, Name = product.Name, Price = product.Price };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

         if(product is  null)
        {
            return false;
        }

        _productRepository.Remove(product);
        await _productRepository.SaveChangesAsync();
         
         return true;
    }
}
