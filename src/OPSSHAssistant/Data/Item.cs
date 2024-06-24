using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

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

    [JsonIgnore]
    public string PublicKeyPath { get; set; } = string.Empty;

    [JsonIgnore]
    public string PublicKey { get; set; } = string.Empty;

    [JsonIgnore]
    public bool ShouldExport { get; set; } = false;

    public string GetDisplayName()
    {
	    return $"{Title} ({Id})";
    }

    public async Task<bool> LoadPublicKeyAsync(Account selectedAccount)
    {	
	    try
	    {
		    var result = await Cli.Wrap("op")
			    .WithArguments($"item get {Id} --vault {Vault.Id} --account {selectedAccount.AccountUuid} --fields \"public_key\" --format json --no-color")
			    .ExecuteBufferedAsync();

		    var publicKey = JsonSerializer.Deserialize<PublicKey>(result.StandardOutput);
			
		    if (publicKey is null || string.IsNullOrEmpty(publicKey.Value))
		    {
			    throw new Exception("Public key is null or empty.");
		    }

		    PublicKey = publicKey.Value;
		    
		    return true;
	    }
	    catch (Exception err)
	    {
		    Debugger.Break();
		    AnsiConsole.MarkupLine($"[red]Error loading public key for {Title}: {err.Message}[/]");
		    return false;
	    }
    }
}