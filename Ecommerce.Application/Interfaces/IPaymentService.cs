using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreateCheckoutSessionAsync(int customerId , PaymentRequest request);
    Task<bool> ProcessStripeWebhookAsync(string rawJson, string stripeSignature);
}
