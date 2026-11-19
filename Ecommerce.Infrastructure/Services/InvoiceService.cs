using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ecommerce.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    public InvoiceService()
    {
        // تفعيل الرخصة المجانية للمجتمع من QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateOrderInvoicePdf(Order order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                // ترويسة الفاتورة (Header)
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("E-COMMERCE PLATFORM").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Invoice #{order.Id}").FontSize(14).SemiBold();
                        col.Item().Text($"Date: {order.CreatedAt:yyyy-MM-dd HH:mm} UTC");
                        col.Item().Text($"Customer: {order.Customer?.FirstName} {order.Customer?.LastName}");
                    });
                });

                // جدول المنتجات (Content)
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Product").SemiBold();
                            header.Cell().AlignRight().Text("Qty").SemiBold();
                            header.Cell().AlignRight().Text("Unit Price").SemiBold();
                            header.Cell().AlignRight().Text("Total").SemiBold();
                        });

                        foreach (var item in order.OrderItems)
                        {
                            var productName = item.Product?.Name ?? $"Product #{item.ProductId}";
                            var lineTotal = item.Price * item.Quantity;

                            table.Cell().PaddingVertical(5).Text(productName);
                            table.Cell().PaddingVertical(5).AlignRight().Text(item.Quantity.ToString());
                            table.Cell().PaddingVertical(5).AlignRight().Text($"${item.Price:F2}");
                            table.Cell().PaddingVertical(5).AlignRight().Text($"${lineTotal:F2}");
                        }
                    });

                    // ملخص الضريبة والإجمالي
                    col.Item().PaddingTop(15).AlignRight().Column(totals =>
                    {
                        var subTotal = order.TotalAmount - order.TaxAmount;
                        totals.Item().Text($"Subtotal: ${subTotal:F2}");
                        totals.Item().Text($"VAT (15%): ${order.TaxAmount:F2}");
                        totals.Item().Text($"Grand Total: ${order.TotalAmount:F2}").FontSize(14).SemiBold().FontColor(Colors.Green.Darken2);
                        totals.Item().Text($"Transaction ID: {order.PaymentTransactionId}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });

                // تذييل الفاتورة (Footer)
                page.Footer().AlignCenter().Text("Thank you for shopping with us! Paid via Stripe.").FontSize(10).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }
}
