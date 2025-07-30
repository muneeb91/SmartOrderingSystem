using SmartOrderingSystem.Models;
using System.Text.Json;

public static class UserHelper
{
    private static readonly string userFilePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "users.json");

    public static List<User> ReadUsers()
    {
        if (!File.Exists(userFilePath))
            return new List<User>();

        var json = File.ReadAllText(userFilePath);
        return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
    }

    public static void WriteUsers(List<User> users)
    {
        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(userFilePath, json);
    }
}
