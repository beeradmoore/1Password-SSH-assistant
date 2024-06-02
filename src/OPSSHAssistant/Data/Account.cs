using System.Text.Json.Serialization;

namespace OPSSHAssistant.Data;

public class Account
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("user_uuid")]
    public string UserUuid { get; set; } = string.Empty;
    
    [JsonPropertyName("account_uuid")]
    public string AccountUuid { get; set; } = string.Empty;
    
    public string GetDisplayName()
    {
        return $"{Email} ({Url})";
    }
}

