using SmartOrderingSystem.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SmartOrderingSystem.Helpers
{
    public static class FileHelper
    {
        private static readonly string ordersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "orders.json");
        private static readonly string menusFilePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "menuitems.json");


        public static List<Order> ReadOrders()
        {
            if (!File.Exists(ordersFilePath))
                return new List<Order>();

            string json = File.ReadAllText(ordersFilePath);
            return JsonSerializer.Deserialize<List<Order>>(json) ?? new List<Order>();
        }

        public static void WriteOrders(List<Order> orders)
        {
            string json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ordersFilePath, json);
        }


        public static List<MenuItem> ReadMenus()
        {
            if (!File.Exists(menusFilePath))
                return new List<MenuItem>();

            string json = File.ReadAllText(menusFilePath);
            return JsonSerializer.Deserialize<List<MenuItem>>(json) ?? new List<MenuItem>();
        }

        public static void WriteMenus(List<MenuItem> menus)
        {
            string json = JsonSerializer.Serialize(menus, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(menusFilePath, json);
        }
    }
}
