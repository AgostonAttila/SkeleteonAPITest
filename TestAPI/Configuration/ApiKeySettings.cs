namespace TestAPI.Configuration;

public class ApiKeySettings
{
    public string Key { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public TimeSpan TokenValidity { get; set; } = TimeSpan.FromMinutes(15);
}
