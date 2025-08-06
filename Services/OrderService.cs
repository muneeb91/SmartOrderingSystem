using SmartOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SmartOrderingSystem.Services
{
    public class OrderService : IOrderService
    {
        private readonly DataContext _context;

        public OrderService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.Items)
                .ToListAsync();
        }

        public async Task<Order> CreateAsync(Order order)
        {
            if (order == null || order.Items == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(order.CustomerName) || string.IsNullOrEmpty(order.CustomerPhone) || order.Items.Count == 0)
            {
                return null;
            }

            if (order.OrderDate == default)
            {
                order.OrderDate = DateTime.UtcNow;
            }

            order.Status = order.Status ?? "Received";

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return false;
            }

            var validStatuses = new[] { "Received", "In Kitchen", "Ready", "Delivered" };
            if (!validStatuses.Contains(status))
            {
                return false;
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<Order> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Order order)
        {
            throw new NotImplementedException();
        }
    }
}