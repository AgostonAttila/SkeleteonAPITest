using Microsoft.Extensions.Logging;
using TestAPI.Entities;

namespace TestAPI.Services;

public interface IUserService
{
    Task<UserAccount?> FindByUsernameAsync(string username);
    bool VerifyPassword(UserAccount user, string password);
    IEnumerable<UserAccount> GetAll();
}

public class UserService : IUserService
{
    private readonly List<UserAccount> _users;
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
        _users = new List<UserAccount>
        {
            new()
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Roles = new List<string> { "Admin", "User" }
            },
            new()
            {
                Username = "staff",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
                Roles = new List<string> { "User" }
            }
        };
    }

    public Task<UserAccount?> FindByUsernameAsync(string username)
    {
        var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public bool VerifyPassword(UserAccount user, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public IEnumerable<UserAccount> GetAll()
    {
        return _users;
    }
}
