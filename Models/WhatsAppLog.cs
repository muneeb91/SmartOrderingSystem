
namespace SmartOrderingSystem.Models
{
    public class WhatsAppLog
    {
        public int Id { get; set; }
        public required string Message { get; set; }
        public required string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public int OrderId { get; set; } 
    }
}