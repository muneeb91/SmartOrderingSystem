using SmartOrderingSystem.Models;
using SmartOrderingSystem.Repositories;
using System.Collections.Generic;

namespace SmartOrderingSystem.Services
{
    public class OrderService
    {
        private OrderRepository orderRepo = new OrderRepository();
        private MenuRepository menuRepo = new MenuRepository();

        public List<Order> GetAll()
        {
            return orderRepo.GetAll();
        }

        public Order GetById(int id)
        {
            return orderRepo.GetById(id);
        }

        public void PlaceOrder(Order order)
        {
            decimal total = 0;

            for (int i = 0; i < order.Items.Count; i++)
            {
                MenuItem m = menuRepo.GetById(order.Items[i].MenuItemId);
                if (m != null)
                {
                    order.Items[i].MenuItemName = m.Name;
                    order.Items[i].Price = m.Price;
                    total += m.Price * order.Items[i].Quantity;
                }
            }

            order.TotalPrice = total;

            orderRepo.Add(order);
        }

        public void UpdateStatus(int id, OrderStatus status)
        {
            orderRepo.UpdateStatus(id, status);
        }
    }
}
