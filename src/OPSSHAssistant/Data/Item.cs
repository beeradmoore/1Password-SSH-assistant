using System.Text.Json.Serialization;

namespace OPSSHAssistant.Data;

public class Item
{
	[JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
	[JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
	[JsonPropertyName("version")]
    public int Version { get; set; }

	[JsonPropertyName("vault")]
	public Vault Vault { get; set; } = new Vault();
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("last_edited_by")]
    public string LastEditedBy { get; set; } = string.Empty;
    
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
    
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("additional_information")]
    public string AdditionalInformation { get; set; } = string.Empty;
}