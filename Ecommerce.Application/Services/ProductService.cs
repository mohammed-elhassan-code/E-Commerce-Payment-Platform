using ecommer.Application.DTOs;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.VisualBasic;

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

    public async Task<int> AddStockAsync(int productid , int quantityToAdd)
    {
        if(quantityToAdd <=0)
          throw new ArgumentException("يجب ان تكون الكمية المضافة اكبر من الصفر");

        var product = await _productRepository.GetByIdAsync(productid);

        if(product is null)
          throw new KeyNotFoundException("المنتج المحدد غير موجود في النظام");
        
        // إضافة الكمية الجديدة إلى المخزون الحالي
        product.Stock+=quantityToAdd;
        
        _productRepository.Update(product);
        await _productRepository.SaveChangesAsync();

        // إرجاع إجمالي المخزون الجديد بعد التحديث
        return product.Stock;

    }

    public async Task<PagedResult<ProductResponse>> GetProductsAsync(ProductQueryParameters parameters)
{
    var (products, totalCount) = await _productRepository.GetFilteredAsync(parameters);

    var productDtos = products.Select(p => new ProductResponse
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        Stock = p.Stock,
        CategoryName = p.Category?.Name ?? "Uncategorized"
    });

    return new PagedResult<ProductResponse>
    {
        Items = productDtos,
        TotalCount = totalCount,
        PageNumber = parameters.PageNumber,
        PageSize = parameters.PageSize
    };
}


    
}
