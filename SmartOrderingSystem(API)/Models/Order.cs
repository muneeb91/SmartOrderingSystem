namespace SmartOrderingSystem.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }  // Add this back!

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        public decimal TotalPrice { get; set; }

        public OrderStatus Status { get; set; }
    }
}
