using System.Text.Json.Serialization;

namespace OPSSHAssistant.Core.Data;

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

    [JsonIgnore]
    public string PublicKeyPath { get; set; } = string.Empty;

    [JsonIgnore]
    public string PublicKey { get; set; } = string.Empty;

    [JsonIgnore]
    public bool NeedsExport { get; set; } = false;

    [JsonIgnore]
    public string Username { get; set; } = "UPDATE_USERNAME_HERE";

    [JsonIgnore]
    public string Host { get; set; } = "UPDATE_HOST_NAME_HERE";
	    
    public string GetDisplayName()
    {
	    return $"{Title} ({Id})";
    }
}