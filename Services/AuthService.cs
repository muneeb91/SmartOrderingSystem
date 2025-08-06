
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartOrderingSystem.Models;
using SmartOrderingSystem.Models.Auth;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;

        public AuthService(IConfiguration configuration, DataContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    Console.WriteLine($"Invalid login request: Username or Password is empty");
                    return new AuthResponse { Error = "Username and Password cannot be empty" };
                }

                Console.WriteLine($"Fetching user: {request.Username}");
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    Console.WriteLine($"User not found: {request.Username}");
                    return new AuthResponse { Error = "Invalid username or password" };
                }

                if (string.IsNullOrEmpty(user.Password))
                {
                    Console.WriteLine($"Empty Password for user: {request.Username}");
                    return new AuthResponse { Error = "User has no password set" };
                }

                try
                {
                    Console.WriteLine($"Verifying password for user: {request.Username}");
                    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                    {
                        Console.WriteLine($"Password verification failed for user: {request.Username}");
                        return new AuthResponse { Error = "Invalid username or password" };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"BCrypt verification error for user: {request.Username}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                    return new AuthResponse { Error = $"Password verification failed: {ex.Message}" };
                }

                Console.WriteLine($"Generating JWT for user: {request.Username}");
                var token = GenerateJwtToken(user);
                Console.WriteLine($"Generated JWT for {request.Username}: {token}");
                var payload = System.Text.Json.JsonSerializer.Serialize(
                    System.Text.Json.JsonSerializer.Deserialize<object>(
                        System.Convert.FromBase64String(token.Split('.')[1].PadRight((token.Split('.')[1].Length + 3) / 4 * 4, '='))));
                Console.WriteLine($"JWT Payload: {payload}");
                return new AuthResponse { Token = token };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in LoginAsync: {ex.Message}, StackTrace: {ex.StackTrace}");
                return new AuthResponse { Error = $"Unexpected error during login: {ex.Message}" };
            }
        }
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    Console.WriteLine($"Invalid register request: Username or Password is empty");
                    return new AuthResponse { Error = "Username and Password cannot be empty" };
                }

                // Validate role
                var validRoles = new[] { "Customer", "Admin", "Kitchen" };
                if (!string.IsNullOrEmpty(request.Role) && !validRoles.Contains(request.Role))
                {
                    Console.WriteLine($"Invalid role for user: {request.Username}, Role: {request.Role}");
                    return new AuthResponse { Error = $"Invalid role. Must be one of: {string.Join(", ", validRoles)}" };
                }

                Console.WriteLine($"Checking database connection for user: {request.Username}");
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex)
                {
                    Console.WriteLine($"Database connection error: {ex.Message}, SqliteErrorCode: {ex.SqliteErrorCode}, StackTrace: {ex.StackTrace}");
                    return new AuthResponse { Error = $"Database connection failed: {ex.Message}" };
                }

                Console.WriteLine($"Checking if username exists: {request.Username}");
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);
                if (existingUser != null)
                {
                    Console.WriteLine($"Username {request.Username} already exists");
                    return new AuthResponse { Error = $"Username {request.Username} already exists" };
                }

                Console.WriteLine($"Hashing password for user: {request.Username}");
                string hashedPassword;
                try
                {
                    hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"BCrypt hashing error for user: {request.Username}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                    return new AuthResponse { Error = $"Password hashing failed: {ex.Message}" };
                }

                var user = new User
                {
                    Username = request.Username,
                    Password = hashedPassword,
                    Role = request.Role ?? "Customer"
                };

                Console.WriteLine($"Adding user to database: {request.Username}, Role: {user.Role}");
                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex)
                {
                    Console.WriteLine($"Database error while saving user: {request.Username}, Error: {ex.Message}, SqliteErrorCode: {ex.SqliteErrorCode}, StackTrace: {ex.StackTrace}");
                    return new AuthResponse { Error = $"Failed to save user to database: {ex.Message}" };
                }

                Console.WriteLine($"User saved: {request.Username}, Role: {user.Role}");
                return new AuthResponse { Success = true };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RegisterAsync: {ex.Message}, StackTrace: {ex.StackTrace}");
                return new AuthResponse { Error = $"Unexpected error during registration: {ex.Message}" };
            }
        }

        private string GenerateJwtToken(User user)
        {
            try
            {
                var jwtKey = _configuration["Jwt:Key"];
                if (string.IsNullOrEmpty(jwtKey))
                {
                    Console.WriteLine("JWT Key is not configured");
                    throw new InvalidOperationException("JWT Key is not configured");
                }
                if (Encoding.UTF8.GetBytes(jwtKey).Length < 32)
                {
                    Console.WriteLine($"JWT Key is too short: {Encoding.UTF8.GetBytes(jwtKey).Length} bytes, expected >= 32 bytes");
                    throw new InvalidOperationException("JWT Key must be at least 32 characters (256 bits) for HS256");
                }

                var claims = new[]
                {
                    new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.Username),
                    new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", user.Role),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iss, _configuration["Jwt:Issuer"] ?? "your-issuer"),
                    new Claim(JwtRegisteredClaimNames.Aud, _configuration["Jwt:Audience"] ?? "your-audience")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"] ?? "your-issuer",
                    audience: _configuration["Jwt:Audience"] ?? "your-audience",
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                Console.WriteLine($"JWT generated for {user.Username}: {tokenString}");
                return tokenString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateJwtToken: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }


 
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public string Error { get; set; }
        public bool Success { get; set; }
    }
}