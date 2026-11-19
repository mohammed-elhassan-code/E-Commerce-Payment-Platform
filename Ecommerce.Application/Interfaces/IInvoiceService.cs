using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IInvoiceService
{
    byte[] GenerateOrderInvoicePdf(Order order);
}