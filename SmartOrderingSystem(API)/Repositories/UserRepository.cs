using SmartOrderingSystem.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SmartOrderingSystem.Repositories
{
    public class UserRepository
    {
        private readonly string usersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "users.json");

        public List<User> GetAllUsers()
        {
            if (!File.Exists(usersFilePath))
                return new List<User>();

            string json = File.ReadAllText(usersFilePath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        public User GetUserByUsername(string username)
        {
            var users = GetAllUsers();
            return users.FirstOrDefault(u => u.Username == username);
        }
    }
}
