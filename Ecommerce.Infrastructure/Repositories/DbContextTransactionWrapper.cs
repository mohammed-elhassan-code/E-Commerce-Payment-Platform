using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ecommerce.Infrastructure.Repositories;

public class DbContextTransactionWrapper : IDbContextTransactionWrapper
{
    private readonly IDbContextTransaction _Transaction;

    public DbContextTransactionWrapper(IDbContextTransaction transaction)
    {
        _Transaction = transaction;
    }

    public async Task CommitAsync() => await _Transaction.CommitAsync();
    public async Task RollBackAsync() => await _Transaction.RollbackAsync();
    public void Dispose() => _Transaction.Dispose();
}