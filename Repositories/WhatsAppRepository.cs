using SmartOrderingSystem.Models;

namespace SmartOrderingSystem.Repositories
{
    public class WhatsAppRepository : IWhatsAppRepository
    {
        private readonly List<WhatsAppLog> _logs = new List<WhatsAppLog>();
        public Task AddLogAsync(WhatsAppLog log)
        {
            log.Id = _logs.Count > 0 ? _logs.Max(l => l.Id) + 1 : 1;
            log.Timestamp = DateTime.UtcNow;
            _logs.Add(log);
            return Task.CompletedTask;
        }
        public Task<List<WhatsAppLog>> GetLogsByOrderIdAsync(int orderId) =>
            Task.FromResult(_logs.Where(l => l.OrderId == orderId).ToList());
    }
}
