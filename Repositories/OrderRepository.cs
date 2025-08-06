using SmartOrderingSystem.Models;

namespace SmartOrderingSystem.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = new List<Order>();
        public Task<List<Order>> GetAllAsync() => Task.FromResult(_orders);
        public Task<Order> GetByIdAsync(int id) => Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));
        public Task AddAsync(Order order)
        {
            order.Id = _orders.Count > 0 ? _orders.Max(o => o.Id) + 1 : 1;
            _orders.Add(order);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Order order)
        {
            var existing = _orders.FirstOrDefault(o => o.Id == order.Id);
            if (existing != null)
            {
                existing.Items = order.Items;
                existing.TotalPrice = order.TotalPrice;
                existing.Status = order.Status;
                existing.CustomerName = order.CustomerName;
                existing.CustomerPhone = order.CustomerPhone;
            }
            return Task.CompletedTask;
        }
    }
}
