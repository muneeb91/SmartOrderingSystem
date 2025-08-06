using SmartOrderingSystem.Models;

namespace SmartOrderingSystem.Repositories
{
    public interface IWhatsAppRepository
    {
        Task AddLogAsync(WhatsAppLog log);
        Task<List<WhatsAppLog>> GetLogsByOrderIdAsync(int orderId);
    }
}
