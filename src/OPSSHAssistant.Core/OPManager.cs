using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using OPSSHAssistant.Core.Data;

namespace OPSSHAssistant.Core;

public class OPManager
{
    public string LastError { get; set; } = string.Empty;

    public OPManager()
    {
    }

    public async Task<bool> CheckFor1PasswordCLIAsync()
    {
        try
        {
            var result = await Cli.Wrap("op")
                .ExecuteAsync();

            return true;
        }
        catch (Exception err)
        {
	        LastError = err.Message;
            Debug.WriteLine($"Error: {err.Message}");
            return false;
        }
    }

    public async Task<List<Account>?> LoadAccountsAsync()
    {
        try
        {
            var result = await Cli.Wrap("op")
                .WithArguments("account list --format json --no-color")
                .ExecuteBufferedAsync();

            if (result.IsSuccess == false)
            {
                var stringBuilder = new StringBuilder();
                
                if (String.IsNullOrEmpty(result.StandardOutput) == false)
                {
                    stringBuilder.AppendLine(result.StandardOutput);
                }

                if (String.IsNullOrEmpty(result.StandardError) == false)
                {
                    stringBuilder.AppendLine(result.StandardError);
                }
                
                LastError = stringBuilder.ToString();
                
                return null;
            }
            
            var accounts = JsonSerializer.Deserialize<List<Account>>(result.StandardOutput);
            if (accounts is null)
            {
                LastError = "Could not load accounts. Are you sure enabled 1Password CLI from within the 1Password desktop application?";
                return null;
            }
            
            accounts.Sort((a, b) => a.Email.CompareTo(b.Email));

            return accounts;
        }
        catch (Exception err)
        {
            LastError = err.Message;
            return null;
        }
    }

    public async Task<List<Vault>?> LoadVaultsAsync(Account account)
    {
	    try
	    {
		    var result = await Cli.Wrap("op")
			    .WithArguments($"vault list --account {account.AccountUuid} --format json --no-color")
			    .ExecuteBufferedAsync();
		    
		    if (result.IsSuccess == false)
		    {
			    var stringBuilder = new StringBuilder();
	        
			    if (String.IsNullOrEmpty(result.StandardOutput) == false)
			    {
				    stringBuilder.AppendLine(result.StandardOutput);
			    }

			    if (String.IsNullOrEmpty(result.StandardError) == false)
			    {
				    stringBuilder.AppendLine(result.StandardError);
			    }
	        
			    LastError = stringBuilder.ToString();
	        
			    return null;
		    }
		    
		    var vaults = JsonSerializer.Deserialize<List<Vault>>(result.StandardOutput);
		    if (vaults is null)
		    {
			    LastError = "Could not load vaults.";
			    return null;
		    }
		    
		    vaults.Sort((a, b) => a.Name.CompareTo(b.Name));

		    return vaults;
	    }
	    catch (Exception err)
	    {
		    LastError = err.Message;
		    return null;
	    }
    }

    public async Task<List<Item>?> LoadItemsAsync(Account? account, Vault? vault)
    {
	    if (account is null)
	    {
		    LastError = "Account not found.";
		    return null;
	    }
	    
	    if (vault is null)
	    {
		    LastError = "Vault not found.";
		    return null;
	    }
	    
	    try
	    {
		    var result = await Cli.Wrap("op")
			    .WithArguments($"item list --account {account.AccountUuid} --vault {vault.Id} --format json --no-color --categories \"SSH Key\"")
			    .ExecuteBufferedAsync();

		    if (result.IsSuccess == false)
		    {
			    var stringBuilder = new StringBuilder();
	        
			    if (String.IsNullOrEmpty(result.StandardOutput) == false)
			    {
				    stringBuilder.AppendLine(result.StandardOutput);
			    }

			    if (String.IsNullOrEmpty(result.StandardError) == false)
			    {
				    stringBuilder.AppendLine(result.StandardError);
			    }
	        
			    LastError = stringBuilder.ToString();
	        
			    return null;
		    }
		    
		    var items = JsonSerializer.Deserialize<List<Item>>(result.StandardOutput);
		    if (items is not null)
		    {
			    items.Sort((a, b) => a.Title.CompareTo(b.Title));
		    }
		    return items;
	    }
	    catch (Exception err)
	    {
		    LastError = err.Message;
		    return null;
	    }
    }
    

	public async Task<bool?> LoadPublicKeysToExportAsync(Account account, Vault vault, List<Item> items)
	{
		if (items.Count == 0)
		{
			return false;
		}
		
		try
		{
			var invalidCharacters = new List<char>();
			invalidCharacters.AddRange(Path.GetInvalidPathChars());
			invalidCharacters.AddRange(Path.GetInvalidFileNameChars());
			invalidCharacters = invalidCharacters.Distinct().ToList();
			
			var needsAnyExport = false;
			
			foreach (var item in items)
			{
				var publicKey = await LoadPublicKeyAsync(account, vault, item);
				if (publicKey is null)
				{
					LastError = $"Error loading public key for {item.Title}.";
					return null;
				}

				if (string.IsNullOrEmpty(publicKey.Value))
				{
					LastError = $"{item.Title} does not have a public key.";
					return null;
				}

				item.PublicKey = publicKey.Value;
				
				var fileName = $"{item.Title}";
				foreach (var invalidCharacter in invalidCharacters)
				{
					if (fileName.Contains(invalidCharacter, StringComparison.OrdinalIgnoreCase))
					{
						fileName = fileName.Replace(invalidCharacter, '_');
					}
				}

				// Try multiple names for export, if we are not able to use any we will fail
				var publicKeyFileNames = new string[]
				{
					$"{fileName}.pub",
					$"{fileName}_{DateTime.Now.ToString("yyyy-MM-dd")}.pub",
					$"{fileName}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.pub",
					$"{fileName}_{Guid.NewGuid().ToString("D")}.pub",
				};

				foreach (var publicKeyFileName in publicKeyFileNames)
				{
					var fullPath = Path.Combine(GetSSHPath(), publicKeyFileName);
					
					if (File.Exists(fullPath))
					{
						var tempPublicKey = File.ReadAllText(fullPath);
						if (tempPublicKey == item.PublicKey)
						{
							item.PublicKeyPath = fullPath;
							item.NeedsExport = false;
							break;
						}
						else
						{
							// NOOP: Try next filename
						}
					}
					else
					{
						item.PublicKeyPath = fullPath;
						item.NeedsExport = true;
						needsAnyExport = true;
						break;
					}
				}

				if (string.IsNullOrEmpty(item.PublicKeyPath))
				{
					LastError = $"Could not find suitable place to export public key for {item.Title}.";
					return null;
				}
			}

			return needsAnyExport;
		}
		catch (Exception err)
		{
			LastError = err.Message;
			return null;
		}
	}
	
	public async Task<PublicKey?> LoadPublicKeyAsync(Account account, Vault vault, Item item)
	{
		try
		{
			var result = await Cli.Wrap("op")
				.WithArguments($"item get {item.Id} --vault {vault.Id} --account {account.AccountUuid} --fields \"public_key\" --format json --no-color")
				.ExecuteBufferedAsync();

			var publicKey = JsonSerializer.Deserialize<PublicKey>(result.StandardOutput);

			if (publicKey is null)
			{
				LastError = "Could not load public key.";
				return null;
			}

			return publicKey;
		}
		catch (Exception err)
		{
			LastError = err.Message;
			return null;
		}
	}

	public string GetAgentTomlDirectory()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\1Password\config\ssh\");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "1Password", "ssh");
			//return "~/.config/1Password/ssh/agent.toml";
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "1Password", "ssh");
			//return "~/.config/1Password/ssh/agent.toml";
		}
		
		throw new Exception("Could not determine 1Password agent.toml path.");
	}
	public string GetAgentTomlPath()
	{
		return Path.Combine(GetAgentTomlDirectory(), "agent.toml");
		
		/*
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\1Password\config\ssh\agent.toml");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "1Password", "ssh", "agent.toml");
			//return "~/.config/1Password/ssh/agent.toml";
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "1Password", "ssh", "agent.toml");
			//return "~/.config/1Password/ssh/agent.toml";
		}
		
		throw new Exception("Could not determine 1Password agent.toml path.");
		*/
	}

	public string GetSSHPath()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			//HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.ssh\");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
		}
		
		throw new Exception("Could not determine SSH config path.");
	}
	
	public string GetSSHConfigPath()
	{
		return Path.Combine(GetSSHPath(), "config");
	}

	public string GenerateUpdatedAgentToml(Account account, Vault vault, List<Item> items)
	{
		var agentTomlStringBuilder = new StringBuilder();
				
		foreach (var item in items)
		{
			agentTomlStringBuilder.AppendLine("[[ssh-keys]]");
			agentTomlStringBuilder.AppendLine($"account = \"{account.AccountUuid}\"");
			agentTomlStringBuilder.AppendLine($"vault = \"{vault.Name}\"");
			agentTomlStringBuilder.AppendLine($"item = \"{item.Title}\"");
			agentTomlStringBuilder.AppendLine("");
		}

		return agentTomlStringBuilder.ToString();
	}
	
	public string GenerateUpdatedSSHConfig(Account accuont, Vault vault, List<Item> items)
	{
		var sshConfigStringBuilder = new StringBuilder();
				
		foreach (var storedItemObject in items)
		{
			sshConfigStringBuilder.AppendLine($"Host UPDATE_HOST_NAME_HERE");
			sshConfigStringBuilder.AppendLine($"  User UPDATE_USERNAME_HERE");
			sshConfigStringBuilder.AppendLine($"  PreferredAuthentications publickey");
			sshConfigStringBuilder.AppendLine($"  IdentityFile \"{storedItemObject.PublicKeyPath}\"");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				sshConfigStringBuilder.AppendLine(@"  IdentityAgent ""\\.\pipe\openssh-ssh-agent""");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				sshConfigStringBuilder.AppendLine($"  IdentityAgent \"~/Library/Group Containers/2BUA8C4S2C.com.1password/t/agent.sock\"");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				sshConfigStringBuilder.AppendLine($"  IdentityAgent \"~/.1password/agent.sock\"");
			}
			sshConfigStringBuilder.AppendLine($"  IdentitiesOnly yes");
			sshConfigStringBuilder.AppendLine();
		}

		return sshConfigStringBuilder.ToString();
	}
	
	
	public async Task<PreparedExport> PrepareExportAsync(Account selectedAccount, Vault selectedVault, List<Item> selectedItemObjects)
	{
		var preparedExport = new PreparedExport();

		var anyPublicKeysNeedExport = await LoadPublicKeysToExportAsync(selectedAccount, selectedVault, selectedItemObjects);
        
		if (anyPublicKeysNeedExport is null)
		{
			preparedExport.Success = false;
			preparedExport.ErrorMessage = "Could not detect public keys to export";
			preparedExport.ErrorMessageDetails = LastError;
			return preparedExport;
		}
        
		if (anyPublicKeysNeedExport == true)
		{           
			foreach (var selectedItemObject in selectedItemObjects)
			{
				if (selectedItemObject.NeedsExport)
				{
					preparedExport.PublicKeysToExport.Add(selectedItemObject);
				}
			}
		}

		preparedExport.SSHConfigToBeCreated = (File.Exists(GetSSHConfigPath()) == false);
		preparedExport.SSHConfigToAppend = GenerateUpdatedSSHConfig(selectedAccount, selectedVault, selectedItemObjects);
        
		preparedExport.AgentTomlToBeCreated = (File.Exists(GetAgentTomlPath()) == false);
		preparedExport.AgentTomlToAppend = GenerateUpdatedAgentToml(selectedAccount, selectedVault, selectedItemObjects);

		preparedExport.Success = true;
        
		return preparedExport;
	}
	
	public async Task<ExportResult> PerformExportAsync(PreparedExport preparedExport)
	{
		var exportResult = new ExportResult();

		exportResult.PublicKeyGenerationSuccess = true;
		foreach (var selectedItemObject in preparedExport.PublicKeysToExport)
		{
			if (selectedItemObject.NeedsExport)
			{
				try
				{
					await File.WriteAllTextAsync(selectedItemObject.PublicKeyPath, selectedItemObject.PublicKey);
				}
				catch (Exception err)
				{
					exportResult.PublicKeyGenerationSuccess = false;
					exportResult.PublicKeyGenerationSummary += $"Error exporting {selectedItemObject.PublicKeyPath}. ({err.Message})\n";
				}
			}
		}
		exportResult.PublicKeyGenerationSummary = exportResult.PublicKeyGenerationSummary.TrimEnd();

		exportResult.AppendSSHConfigSuccess = true;
		if (preparedExport.SSHConfigToBeCreated)
		{
			try
			{
				if (Directory.Exists(GetSSHPath()) == false)
				{
					Directory.CreateDirectory(GetSSHPath());
				}
				
				await File.WriteAllTextAsync(GetSSHConfigPath(), preparedExport.SSHConfigToAppend);
			}
			catch (Exception err)
			{
				exportResult.AppendSSHConfigSuccess = false;
				exportResult.SSHConfigSummary = $"Error creating SSH config. ({err.Message})";
			}
		}
		else
		{
			try
			{
				await File.AppendAllTextAsync(GetSSHConfigPath(), "\n\n" + preparedExport.SSHConfigToAppend);
			}
			catch (Exception err)
			{
				exportResult.AppendSSHConfigSuccess = false;
				exportResult.SSHConfigSummary = $"Error appending SSH config. ({err.Message})";
			}
		}

		exportResult.AppendAgentTomlSuccess = true;
		if (preparedExport.AgentTomlToBeCreated)
		{
			try
			{
				if (Directory.Exists(GetAgentTomlDirectory()) == false)
				{
					Directory.CreateDirectory(GetAgentTomlDirectory());
				}
				
				await File.WriteAllTextAsync(GetAgentTomlPath(), preparedExport.AgentTomlToAppend);
			}
			catch (Exception err)
			{
				exportResult.AppendAgentTomlSuccess = false;
				exportResult.AgentTomlSummary = $"Error creating agent.toml. ({err.Message})";
			}
		}
		else
		{
			try
			{
				await File.AppendAllTextAsync(GetAgentTomlPath(), "\n\n" + preparedExport.AgentTomlToAppend);
			}
			catch (Exception err)
			{
				exportResult.AppendAgentTomlSuccess = false;
				exportResult.AgentTomlSummary = $"Error appending agent.toml. ({err.Message})";
			}
		}

		return exportResult;
	}
}