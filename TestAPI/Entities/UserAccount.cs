namespace TestAPI.Entities;

public class UserAccount
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
