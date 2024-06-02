using System.Text.Json.Serialization;

namespace OPSSHAssistant.Data;

public class PublicKey
{
	[JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
	[JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
	[JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
    
	[JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

	[JsonPropertyName("reference")]
	public string Reference { get; set; } = string.Empty;
}
