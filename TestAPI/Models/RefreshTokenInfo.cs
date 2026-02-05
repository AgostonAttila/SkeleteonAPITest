namespace TestAPI.Models;

public class RefreshTokenInfo
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
}
