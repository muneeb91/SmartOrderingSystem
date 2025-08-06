
using SmartOrderingSystem.Models.Auth;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
    }
}