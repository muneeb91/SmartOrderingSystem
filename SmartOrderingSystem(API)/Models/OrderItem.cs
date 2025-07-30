namespace SmartOrderingSystem.Models
{
    public class OrderItem
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
