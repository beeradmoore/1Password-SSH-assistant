using System.Diagnostics;
using System.Globalization;
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
    public OPManager()
    {
    }

    public async Task<OPResult<bool>> CheckFor1PasswordCLIAsync()
    {
        try
        {
            var result = await Cli.Wrap("op")
	            .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync().ConfigureAwait(false);

            if (result.IsSuccess)
            {
                return OPResult<bool>.FromSuccess(true);
            }

            Debugger.Break();

            // Fails with exit code 141

            // Sometimes this needs to run twice
            result = await Cli.Wrap("op")
	            .WithValidation(CommandResultValidation.None)
	            .ExecuteBufferedAsync().ConfigureAwait(false);

            Debugger.Break();

            if (result.IsSuccess)
            {
                return OPResult<bool>.FromSuccess(true);
            }


            var stringBuilder = new StringBuilder();

            if (String.IsNullOrEmpty(result.StandardOutput) == false)
            {
                stringBuilder.AppendLine(result.StandardOutput);
            }

            if (String.IsNullOrEmpty(result.StandardError) == false)
            {
                stringBuilder.AppendLine(result.StandardError);
            }

            throw new Exception(stringBuilder.ToString());
        }
        catch (Exception err)
        {
            Debug.WriteLine($"Error: {err.Message}");
            return OPResult<bool>.FromFailed(err.Message);
        }
    }

    public async Task<OPResult<List<Account>>> LoadAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Cli.Wrap("op")
                .WithArguments("account list --format json --no-color")
                .ExecuteBufferedAsync(Encoding.UTF8, cancellationToken);

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

                return OPResult<List<Account>>.FromFailed(stringBuilder.ToString());
            }

            var accounts = JsonSerializer.Deserialize<List<Account>>(result.StandardOutput, SourceGenerationContext.Default.ListAccount);
            if (accounts is null)
            {
                return OPResult<List<Account>>.FromFailed("Could not load accounts. Are you sure enabled 1Password CLI from within the 1Password desktop application?");
            }

            accounts.Sort((a, b) => String.Compare(a.Email, b.Email, StringComparison.Ordinal));

            return OPResult<List<Account>>.FromSuccess(accounts);
        }
        catch (OperationCanceledException)
        {
            return OPResult<List<Account>>.FromCancelled();
        }
        catch (Exception err)
        {
            return OPResult<List<Account>>.FromFailed(err.Message);;
        }
    }

    public async Task<OPResult<List<Vault>>> LoadVaultsAsync(Account account)
    {
	    try
	    {
		    var result = await Cli.Wrap("op")
			    .WithArguments($"vault list --account {account.AccountUuid} --format json --no-color")
			    .ExecuteBufferedAsync(Encoding.UTF8);

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

			    return OPResult<List<Vault>>.FromFailed(stringBuilder.ToString());
		    }

		    var vaults = JsonSerializer.Deserialize<List<Vault>>(result.StandardOutput, SourceGenerationContext.Default.ListVault);
		    if (vaults is null)
		    {
                return OPResult<List<Vault>>.FromFailed("Could not load vaults.");
		    }

		    vaults.Sort((a, b) => a.Name.CompareTo(b.Name, StringComparison.InvariantCultureIgnoreCase));

		    return OPResult<List<Vault>>.FromSuccess(vaults);
	    }
	    catch (Exception err)
	    {
            Debug.WriteLine($"Error: {err.Message}");
		    return OPResult<List<Vault>>.FromFailed(err.Message);
	    }
    }

    public async Task<OPResult<List<Item>>> LoadItemsAsync(Account? account, Vault? vault)
    {
	    if (account is null)
	    {
		    return OPResult<List<Item>>.FromFailed("Account not found.");
	    }

	    if (vault is null)
	    {
            return OPResult<List<Item>>.FromFailed("Vault not found.");
	    }

	    try
	    {
		    var result = await Cli.Wrap("op")
			    .WithArguments($"item list --account {account.AccountUuid} --vault {vault.Id} --format json --no-color --categories \"SSH Key\"")
			    .ExecuteBufferedAsync(Encoding.UTF8);

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

                return OPResult<List<Item>>.FromFailed(stringBuilder.ToString());
		    }

		    var items = JsonSerializer.Deserialize<List<Item>>(result.StandardOutput, SourceGenerationContext.Default.ListItem);

            if (items is null)
            {
                throw new Exception("Could not deserialize items.");
            }

            items.Sort((a, b) => a.Title.CompareTo(b.Title, StringComparison.InvariantCultureIgnoreCase));

            return OPResult<List<Item>>.FromSuccess(items);
	    }
	    catch (Exception err)
	    {
            return OPResult<List<Item>>.FromFailed(err.Message);
	    }
    }


	public async Task<OPResult<bool>> LoadPublicKeysToExportAsync(Account account, Vault vault, List<Item> items)
	{
		if (items.Count == 0)
		{
			return OPResult<bool>.FromFailed("No items found.");
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
				if (publicKey.Success == false || publicKey.Data is null)
				{
					return OPResult<bool>.FromFailed($"Error loading public key for {item.Title}.");
				}

				if (string.IsNullOrEmpty(publicKey.Data.Value))
				{
                    return OPResult<bool>.FromFailed($"{item.Title} does not have a public key.");
				}

				item.PublicKey = publicKey.Data.Value;

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
					$"{fileName}_{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.pub",
					$"{fileName}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}.pub",
					$"{fileName}_{Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture)}.pub",
				};

				foreach (var publicKeyFileName in publicKeyFileNames)
				{
					var fullPath = Path.Combine(GetSSHPath(), publicKeyFileName);

					if (File.Exists(fullPath))
					{
						var tempPublicKey = await File.ReadAllTextAsync(fullPath);
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
                    return OPResult<bool>.FromFailed($"Could not find suitable place to export public key for {item.Title}.");
				}
			}

			return OPResult<bool>.FromSuccess(needsAnyExport);
		}
		catch (Exception err)
		{
			return OPResult<bool>.FromFailed(err.Message);
		}
	}

	public async Task<OPResult<PublicKey>> LoadPublicKeyAsync(Account account, Vault vault, Item item)
	{
		try
		{
			var result = await Cli.Wrap("op")
				.WithArguments($"item get {item.Id} --vault {vault.Id} --account {account.AccountUuid} --fields \"public_key\" --format json --no-color")
				.ExecuteBufferedAsync(Encoding.UTF8);

			var publicKey = JsonSerializer.Deserialize<PublicKey>(result.StandardOutput, SourceGenerationContext.Default.PublicKey);

			if (publicKey is null)
			{
				return OPResult<PublicKey>.FromFailed("Could not load public key.");
			}

			return OPResult<PublicKey>.FromSuccess(publicKey);
		}
		catch (Exception err)
		{
            return OPResult<PublicKey>.FromFailed(err.Message);
		}
	}

	public string GetAgentTomlDirectory()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\1Password\config\ssh\");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
		         RuntimeInformation.RuntimeIdentifier.StartsWith("maccatalyst", StringComparison.OrdinalIgnoreCase))
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
	}

	public string GetSSHPath()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			//HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.ssh\");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
		         RuntimeInformation.RuntimeIdentifier.StartsWith("maccatalyst", StringComparison.OrdinalIgnoreCase))
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
			agentTomlStringBuilder.AppendLine(CultureInfo.InvariantCulture, $"account = \"{account.AccountUuid}\"");
			agentTomlStringBuilder.AppendLine(CultureInfo.InvariantCulture, $"vault = \"{vault.Name}\"");
			agentTomlStringBuilder.AppendLine(CultureInfo.InvariantCulture, $"item = \"{item.Title}\"");
			agentTomlStringBuilder.AppendLine("");
		}

		return agentTomlStringBuilder.ToString();
	}

	public string GenerateUpdatedSSHConfig(Account account, Vault vault, List<Item> items)
	{
		var sshConfigStringBuilder = new StringBuilder();

		foreach (var storedItemObject in items)
		{
			sshConfigStringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Host {storedItemObject.Host}");
			sshConfigStringBuilder.AppendLine(CultureInfo.InvariantCulture, $"  User {storedItemObject.Username}");
			sshConfigStringBuilder.AppendLine(CultureInfo.InvariantCulture, $"  PreferredAuthentications publickey");
			sshConfigStringBuilder.AppendLine(CultureInfo.InvariantCulture, $"  IdentityFile \"{storedItemObject.PublicKeyPath}\"");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
                // Windows OpenSSH agent no longer wants this line appended.
                // https://github.com/beeradmoore/1Password-SSH-assistant/issues/8
                //sshConfigStringBuilder.AppendLine(@"  IdentityAgent ""\\.\pipe\openssh-ssh-agent""");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                     RuntimeInformation.RuntimeIdentifier.StartsWith("maccatalyst", StringComparison.OrdinalIgnoreCase))
			{
				sshConfigStringBuilder.AppendLine("  IdentityAgent \"~/Library/Group Containers/2BUA8C4S2C.com.1password/t/agent.sock\"");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				sshConfigStringBuilder.AppendLine("  IdentityAgent \"~/.1password/agent.sock\"");
			}
			sshConfigStringBuilder.AppendLine("  IdentitiesOnly yes");
			sshConfigStringBuilder.AppendLine();
		}

		return sshConfigStringBuilder.ToString();
	}


	public async Task<PreparedExport> PrepareExportAsync(Account selectedAccount, Vault selectedVault, List<Item> selectedItemObjects)
	{
		var preparedExport = new PreparedExport();

		var anyPublicKeysNeedExport = await LoadPublicKeysToExportAsync(selectedAccount, selectedVault, selectedItemObjects);

		if (anyPublicKeysNeedExport.Success == false)
		{
			preparedExport.Success = false;
			preparedExport.ErrorMessage = "Could not detect public keys to export";
			preparedExport.ErrorMessageDetails = anyPublicKeysNeedExport.ErrorMessage;
			return preparedExport;
		}

		if (anyPublicKeysNeedExport.Data == true)
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
