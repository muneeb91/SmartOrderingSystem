using SmartOrderingSystem.Models;
using System.Collections.Generic;

namespace SmartOrderingSystem.Data
{
    public static class UserStore
    {
        public static List<User> Users = new List<User>
        {
            new User { Username = "admin", PasswordHash = "admin123", Role = "Admin" },
            new User { Username = "kitchen", PasswordHash = "kitchen123", Role = "Kitchen" },
            new User { Username = "customer", PasswordHash = "cust123", Role = "Customer" }
        };
    }
}
