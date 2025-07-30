using SmartOrderingSystem.Helpers;
using SmartOrderingSystem.Models;
using System.Collections.Generic;
using System.Linq;

namespace SmartOrderingSystem.Repositories
{
    public class OrderRepository
    {
        private const string FilePath = "App_Data/orders.json";

        public List<Order> GetAll()
        {
            return FileHelper.ReadOrders();
        }

        public Order GetById(int id)
        {
            var orders = FileHelper.ReadOrders();
            return orders.FirstOrDefault(o => o.Id == id);
        }

        public void Add(Order order)
        {
            var orders = FileHelper.ReadOrders();
            order.Id = orders.Count > 0 ? orders.Max(o => o.Id) + 1 : 1;
            orders.Add(order);
            FileHelper.WriteOrders(orders);
        }

        public void UpdateStatus(int id, OrderStatus status)
        {
            var orders = FileHelper.ReadOrders();
            var order = orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                order.Status = status;
                FileHelper.WriteOrders(orders);
            }
        }
    }
}
