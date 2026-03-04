namespace HealthyCron.Models;

public class TempTelegramHandshake
{
    public string Token { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string? ChatName { get; set; }
    public string? ChatType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
