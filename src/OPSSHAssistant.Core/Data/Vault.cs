using System.Text.Json.Serialization;

namespace OPSSHAssistant.Core.Data;

public class Vault
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;
	
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("content_version")]
	public int ContentVersion { get; set; }
	
	public string GetDisplayName()
	{
		return $"{Name} ({Id})";
	}

	public override string ToString()
	{
		return GetDisplayName();
	}
}
