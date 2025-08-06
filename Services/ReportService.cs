using SmartOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout;
using iText.Layout.Element;
using System.Text.Json;

namespace SmartOrderingSystem.Services
{
    public class ReportService : IReportService
    {
        private readonly DataContext _context;

        public ReportService(DataContext context)
        {
            _context = context;
        }

        public async Task<DailySummaryReport> GetDailySummaryAsync(DateTime date)
        {
            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);
                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.OrderDate >= startDate && o.OrderDate < endDate)
                    .ToListAsync();
                var totalRevenue = orders.Sum(o => o.TotalPrice);
                Console.WriteLine($"GetDailySummaryAsync Data: Orders={orders.Count}, TotalRevenue={totalRevenue}");
                return new DailySummaryReport
                {
                    Date = date,
                    TotalOrders = orders.Count,
                    TotalRevenue = totalRevenue,
                    Orders = orders.Select(o => new OrderSummary
                    {
                        Id = o.Id,
                        CustomerName = SanitizeString(o.CustomerName) ?? "Unknown",
                        Status = SanitizeString(o.Status) ?? "Unknown",
                        TotalPrice = o.TotalPrice,
                        OrderDate = o.OrderDate,
                        Items = o.Items?.Select(i => new OrderItemSummary
                        {
                            Name = SanitizeString(i.Name) ?? "Unknown",
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList() ?? new List<OrderItemSummary>()
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetDailySummaryAsync Error: Message={ex.Message}, StackTrace={ex.StackTrace}, InnerException={ex.InnerException?.Message}");
                throw new Exception($"Failed to fetch daily summary: {ex.Message}", ex);
            }
        }

        public async Task<OrderInvoice> GetOrderInvoiceAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                {
                    Console.WriteLine($"GetOrderInvoiceAsync Error: Order {orderId} not found.");
                    throw new Exception($"Order with ID {orderId} not found.");
                }
                Console.WriteLine($"GetOrderInvoiceAsync Data: {JsonSerializer.Serialize(order)}");
                return new OrderInvoice
                {
                    OrderId = order.Id,
                    CustomerName = SanitizeString(order.CustomerName) ?? "Unknown",
                    CustomerPhone = SanitizeString(order.CustomerPhone) ?? "N/A",
                    OrderDate = order.OrderDate,
                    Status = SanitizeString(order.Status) ?? "Unknown",
                    TotalPrice = order.TotalPrice,
                    Items = order.Items?.Select(i => new OrderItemSummary
                    {
                        Name = SanitizeString(i.Name) ?? "Unknown",
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList() ?? new List<OrderItemSummary>(),
                    CreatedAt = order.OrderDate,
                    DeliveredAt = order.Status == "Delivered" ? DateTime.UtcNow : null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOrderInvoiceAsync Error: Message={ex.Message}, StackTrace={ex.StackTrace}, InnerException={ex.InnerException?.Message}");
                throw new Exception($"Failed to fetch order invoice: {ex.Message}", ex);
            }
        }

        private string SanitizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            // Remove non-printable and special characters
            return string.Concat(input.Where(c => c >= 32 && c <= 126)).Trim();
        }

        public async Task<byte[]> GenerateDailySummaryPdfAsync(DateTime date)
        {
            try
            {
                var report = await GetDailySummaryAsync(date);
                if (report == null || report.Orders == null || !report.Orders.Any())
                {
                    Console.WriteLine($"GenerateDailySummaryPdfAsync Error: No orders found for date {date:yyyy-MM-dd}.");
                    throw new Exception("No orders available for the selected date.");
                }
                Console.WriteLine($"GenerateDailySummaryPdfAsync Data: {JsonSerializer.Serialize(report)}");
                using (var stream = new MemoryStream())
                {
                    using (var writer = new PdfWriter(stream))
                    {
                        using (var pdf = new PdfDocument(writer))
                        {
                            var document = new Document(pdf);
                            PdfFont font;
                            try
                            {
                                font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                                PdfFontFactory.RegisterSystemDirectories();
                            }
                            catch (Exception fontEx)
                            {
                                Console.WriteLine($"GenerateDailySummaryPdfAsync Font Error: Message={fontEx.Message}, StackTrace={fontEx.StackTrace}");
                                throw new Exception("Failed to load Helvetica font.", fontEx);
                            }
                            document.Add(new Paragraph($"Daily Summary Report - {report.Date:yyyy-MM-dd}")
                                .SetFontSize(16)
                                .SetFont(font));
                            document.Add(new Paragraph($"Total Orders: {report.TotalOrders}").SetFont(font));
                            document.Add(new Paragraph($"Total Revenue: ${report.TotalRevenue:F2}").SetFont(font));
                            foreach (var order in report.Orders)
                            {
                                document.Add(new Paragraph($"Order ID: {order.Id}, Customer: {order.CustomerName}, Total: ${order.TotalPrice:F2}")
                                    .SetFont(font));
                                foreach (var item in order.Items)
                                {
                                    document.Add(new Paragraph($"  Item: {item.Name}, Qty: {item.Quantity}, Price: ${item.Price:F2}")
                                        .SetFont(font));
                                }
                            }
                            document.Close();
                        }
                    }
                    var result = stream.ToArray();
                    Console.WriteLine($"GenerateDailySummaryPdfAsync Success: PDF size={result.Length} bytes");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GenerateDailySummaryPdfAsync Error: Message={ex.Message}, StackTrace={ex.StackTrace}, InnerException={ex.InnerException?.Message}");
                throw new Exception($"Failed to generate daily summary PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateOrderInvoicePdfAsync(int orderId)
        {
            try
            {
                var order = await GetOrderInvoiceAsync(orderId);
                if (order == null)
                {
                    Console.WriteLine($"GenerateOrderInvoicePdfAsync Error: Order {orderId} not found.");
                    throw new Exception($"Order with ID {orderId} not found.");
                }
                Console.WriteLine($"GenerateOrderInvoicePdfAsync Data: {JsonSerializer.Serialize(order)}");
                using (var stream = new MemoryStream())
                {
                    using (var writer = new PdfWriter(stream))
                    {
                        using (var pdf = new PdfDocument(writer))
                        {
                            var document = new Document(pdf);
                            PdfFont font;
                            try
                            {
                                font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                                PdfFontFactory.RegisterSystemDirectories();
                            }
                            catch (Exception fontEx)
                            {
                                Console.WriteLine($"GenerateOrderInvoicePdfAsync Font Error: Message={fontEx.Message}, StackTrace={fontEx.StackTrace}");
                                throw new Exception("Failed to load Helvetica font.", fontEx);
                            }
                            document.Add(new Paragraph($"Order Invoice - Order ID: {order.OrderId}")
                                .SetFontSize(16)
                                .SetFont(font));
                            document.Add(new Paragraph($"Customer: {order.CustomerName}").SetFont(font));
                            document.Add(new Paragraph($"Phone: {order.CustomerPhone}").SetFont(font));
                            document.Add(new Paragraph($"Date: {order.OrderDate:yyyy-MM-dd HH:mm:ss}").SetFont(font));
                            document.Add(new Paragraph($"Status: {order.Status}").SetFont(font));
                            document.Add(new Paragraph($"Total: ${order.TotalPrice:F2}").SetFont(font));
                            foreach (var item in order.Items)
                            {
                                document.Add(new Paragraph($"Item: {item.Name}, Quantity: {item.Quantity}, Price: ${item.Price:F2}").SetFont(font));
                            }
                            document.Close();
                        }
                    }
                    var result = stream.ToArray();
                    Console.WriteLine($"GenerateOrderInvoicePdfAsync Success: PDF size={result.Length} bytes");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GenerateOrderInvoicePdfAsync Error: Message={ex.Message}, StackTrace={ex.StackTrace}, InnerException={ex.InnerException?.Message}");
                throw new Exception($"Failed to generate invoice PDF: {ex.Message}", ex);
            }
        }
    }
}