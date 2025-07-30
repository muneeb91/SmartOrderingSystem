using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

public static class DemoJwt
{
    public static string Generate(string username, string role)
    {
        var payload = new
        {
            Username = username,
            Role = role,
            Exp = DateTime.UtcNow.AddHours(1)
        };

        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static (string Username, string Role)? Validate(string token)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return (payload["Username"].ToString(), payload["Role"].ToString());
        }
        catch
        {
            return null;
        }
    }
}
