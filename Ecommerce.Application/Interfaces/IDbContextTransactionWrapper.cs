namespace Ecommerce.Application.Interfaces;

public interface IDbContextTransactionWrapper : IDisposable
{
    Task CommitAsync();
    Task RollBackAsync();
}