
using Microsoft.AspNetCore.Mvc;
using SmartOrderingSystem.Models.Auth;
using SmartOrderingSystem.Services;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Temporary endpoint in AuthController.cs for testing
        [HttpGet("hash-password/{password}")]
        public IActionResult HashPassword(string password)
        {
            return Ok(BCrypt.Net.BCrypt.HashPassword(password));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            if (!string.IsNullOrEmpty(response.Error))
            {
                return BadRequest(new { error = response.Error });
            }
            return Ok(new { token = response.Token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);
            if (!string.IsNullOrEmpty(response.Error))
            {
                return BadRequest(new { error = response.Error });
            }
            return Ok(new { success = response.Success });
        }
    }
}