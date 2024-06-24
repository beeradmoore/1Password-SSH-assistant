using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

namespace OPSSHAssistant.Data;

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

	public async Task SelectItemsAsync(Account selectedAccount)
	{
		try
		{
			var result = await Cli.Wrap("op")
				.WithArguments($"item list --account {selectedAccount.AccountUuid} --vault {Id} --format json --no-color --categories \"SSH Key\"")
				.ExecuteBufferedAsync();

			var items = JsonSerializer.Deserialize<Item[]>(result.StandardOutput);
			
			if (items is null || items.Length == 0)
			{
				AnsiConsoleHelper.DisplayErrorAndContinue("No SSH keys.");
				return;
			}
			
			var itemsDictionary = new Dictionary<string, Item>();
			foreach (var item in items)
			{
				itemsDictionary.Add(item.GetDisplayName(), item);
			}

			var selectedItems = AnsiConsole.Prompt(
				new MultiSelectionPrompt<string>()
					.Title("Select items you want to generate SSH configs for:")
					.PageSize(10)
					.InstructionsText(
						"[grey](Press [blue]<space>[/] to toggle an item, " + 
						"[green]<enter>[/] to generate configs)[/]")
					.AddChoices(itemsDictionary.Keys)
			);

			if (selectedItems.Count == 0)
			{
				return;
			}
			
			var selectedItemObjects = new List<Item>();
			foreach (string selectedItem in selectedItems)
			{
				selectedItemObjects.Add(itemsDictionary[selectedItem]);
			}
			
			var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var sshDirectory = Path.Combine(homeDirectory, ".ssh");
			if (Directory.Exists(sshDirectory))
			{
				var invalidCharacters = new List<char>();
				invalidCharacters.AddRange(Path.GetInvalidPathChars());
				invalidCharacters.AddRange(Path.GetInvalidFileNameChars());
				invalidCharacters = invalidCharacters.Distinct().ToList();

				var shouldAttemptExport = false;
				
				foreach (var selectedItemObject in selectedItemObjects)
				{
					await selectedItemObject.LoadPublicKeyAsync(selectedAccount);

					var fileName = selectedItemObject.Title;
					foreach (var invalidCharacter in invalidCharacters)
					{
						if (fileName.Contains(invalidCharacter, StringComparison.OrdinalIgnoreCase))
						{
							fileName = fileName.Replace(invalidCharacter, '_');
						}
					}

					if (String.Equals(Path.GetExtension(fileName), ".pub", StringComparison.OrdinalIgnoreCase) == false)
					{
						fileName += ".pub";
					}

					var fullPath = Path.Combine(sshDirectory, fileName);
					if (File.Exists(fullPath))
					{
						var tempPublicKey = File.ReadAllText(fullPath);
						if (tempPublicKey == selectedItemObject.PublicKey)
						{
							AnsiConsole.MarkupLine($"[green]Skip {fullPath} (file already exists, but already matches what is stored in 1Password)[/]");
						}
						else
						{
							AnsiConsole.MarkupLine($"[red]Skip {fullPath} (file already exists, but does not match what is stored in 1Password)[/]");
						}
					}
					else
					{
						AnsiConsole.MarkupLine($"[green]Export {fullPath}[/]");
						selectedItemObject.PublicKeyPath = fullPath;
						shouldAttemptExport = true;
					}
				}

				if (shouldAttemptExport)
				{
					var exportPublicKeys = AnsiConsole.Confirm("Export public keys?");
					if (exportPublicKeys)
					{
						foreach (var selectedItemObject in selectedItemObjects)
						{
							if (String.IsNullOrEmpty(selectedItemObject.PublicKeyPath) == false)
							{
								try
								{
									File.WriteAllText(selectedItemObject.PublicKeyPath, selectedItemObject.PublicKey);
								}
								catch (Exception err)
								{
									Debugger.Break();
									AnsiConsole.MarkupLine($"[red]Error: Could not export {selectedItemObject.PublicKeyPath}. ({err.Message})[/]");
								}
							}
						}
					}
				}
				else
				{
					AnsiConsole.Markup("[red]Skipping. No new keys would be exported.[/]");
				}

				var agentTomlStringBuilder = new StringBuilder();
				var sshConfigStringBuilder = new StringBuilder();
				
				foreach (var storedItemObject in selectedItemObjects)
				{
					/*
					if (string.IsNullOrEmpty(storedItemObject.PublicKeyPath))
					{
						continue;
					}
					*/

					agentTomlStringBuilder.AppendLine("[[ssh-keys]]");
					agentTomlStringBuilder.AppendLine($"account = \"{selectedAccount.AccountUuid}\"");
					agentTomlStringBuilder.AppendLine($"vault = \"{Name}\"");
					agentTomlStringBuilder.AppendLine($"item = \"{storedItemObject.Title}\"");
					agentTomlStringBuilder.AppendLine("");
					
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
					else
					{
						sshConfigStringBuilder.AppendLine($"  IdentityAgent \"~/.1password/agent.sock\"");
					}
					sshConfigStringBuilder.AppendLine($"  IdentitiesOnly yes");
					sshConfigStringBuilder.AppendLine();
				}
				
				Console.WriteLine();
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					var tomlPath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\1Password\config\ssh\agent.toml");
					Console.WriteLine($"The following config belongs in {tomlPath}");
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Console.WriteLine("The following config belongs in ~/.config/1Password/ssh/agent.toml");
				}
				else
				{
					Console.WriteLine("The following config belongs in ~/.config/1Password/ssh/agent.toml");
				}
				
				Console.WriteLine(agentTomlStringBuilder.ToString());
				Console.WriteLine();
				
				Console.WriteLine("The following config belongs in ~/.ssh/config");
				Console.WriteLine(sshConfigStringBuilder.ToString());
				Console.WriteLine();

				Environment.Exit(0);
			}
			else
			{
				AnsiConsole.Markup("[red]SSH directory does not exist. Skipping public key generation.[/]");
			}
			
		}
		catch (Exception err)
		{
			Debugger.Break();
			AnsiConsole.MarkupLine($"[red]Error: {err.Message}[/]");
			Environment.Exit(1);
		}
		



	}
}
