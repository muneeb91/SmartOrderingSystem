
using SmartOrderingSystem.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartOrderingSystem.Services
{
    public interface IReportService
    {
        Task<DailySummaryReport> GetDailySummaryAsync(DateTime date);
        Task<OrderInvoice> GetOrderInvoiceAsync(int orderId);
        Task<byte[]> GenerateDailySummaryPdfAsync(DateTime date);
        Task<byte[]> GenerateOrderInvoicePdfAsync(int orderId);
    }

    public class DailySummaryReport
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<OrderSummary> Orders { get; set; }
    }

    public class OrderSummary
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItemSummary> Items { get; set; }
    }

    public class OrderItemSummary
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderInvoice
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemSummary> Items { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
