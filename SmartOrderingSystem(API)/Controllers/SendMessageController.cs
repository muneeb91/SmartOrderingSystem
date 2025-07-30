using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class SendMessageController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public SendMessageController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public class WhatsAppMessage
    {
        public int OrderID { get; set; }
        public string To { get; set; }
        public string Message { get; set; }
        public string Status { get; set; } = "Sent";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    [Authorize(Roles = "Admin,Kitchen")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] WhatsAppMessage input)
    {
        string path = Path.Combine(_env.WebRootPath ?? "App_Data", "whatsapp_logs.json");

        List<WhatsAppMessage> logs = new();
        if (System.IO.File.Exists(path))
        {
            var json = await System.IO.File.ReadAllTextAsync(path);
            if (!string.IsNullOrWhiteSpace(json))
                logs = JsonSerializer.Deserialize<List<WhatsAppMessage>>(json) ?? new();
        }

        logs.Add(input);
        var updatedJson = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(path, updatedJson);

        return Ok(new { message = "Notification sent and logged." });
    }
}
