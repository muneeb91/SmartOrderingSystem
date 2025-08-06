using SmartOrderingSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Services
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllAsync();
        Task<Order> GetByIdAsync(int id);
        Task<Order> CreateAsync(Order order);
        Task AddAsync(Order order);
        Task<bool> UpdateOrderStatusAsync(int id, string status);
    }
}