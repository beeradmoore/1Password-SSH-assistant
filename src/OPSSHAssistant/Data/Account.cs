using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

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

    public static async Task SelectAccountAsync()
    {
        try
        {
            while (true)
            {
                var result = await Cli.Wrap("op")
                    .WithArguments("account list --format json --no-color")
                    .ExecuteBufferedAsync();

                if (result.IsSuccess == false)
                {
                    AnsiConsole.MarkupLine("[red]Error: Could not list accounts.[/]");
                    if (String.IsNullOrEmpty(result.StandardOutput) == false)
                    {
                        AnsiConsole.WriteLine(result.StandardOutput);
                    }

                    if (String.IsNullOrEmpty(result.StandardError) == false)
                    {
                        AnsiConsole.WriteLine(result.StandardError);
                    }

                    Environment.Exit(1);
                }

                var accounts = JsonSerializer.Deserialize<Account[]>(result.StandardOutput);
                if (accounts is null || accounts.Length == 0)
                {
                    AnsiConsole.MarkupLine("[red]Error: No accounts found.[/]");
                    Environment.Exit(0);
                }

                var accountsDictionary = new Dictionary<string, Account>();
                var accountOptions = new List<string>();
                foreach (var account in accounts)
                {
                    accountsDictionary.Add(account.GetDisplayName(), account);
                }

                accountOptions.AddRange(accountsDictionary.Keys);
                accountOptions.Add("Quit");

                var prompt = new SelectionPrompt<string>()
                    .Title("Select your account:")
                    .AddChoices(accountOptions);

                var response = AnsiConsole.Prompt(prompt);

                if (response == "Quit")
                {
                    Environment.Exit(0);
                }

                var selectedAccount = accountsDictionary[response];
                await selectedAccount.SelectVaultAsync();
            }
        }
        catch (Exception err)
        {
            AnsiConsole.MarkupLine($"[red]Error: {err.Message}[/]");
            Environment.Exit(1);
        }
    }

    public async Task SelectVaultAsync()
    {
        try
        {
            while (true)
            {
                var result = await Cli.Wrap("op")
                    .WithArguments($"vault list --account {AccountUuid} --format json --no-color")
                    .ExecuteBufferedAsync();

                if (result.IsSuccess == false)
                {
                    AnsiConsole.MarkupLine("[red]Error: Could not list vaults.[/]");
                    if (String.IsNullOrEmpty(result.StandardOutput) == false)
                    {
                        AnsiConsole.WriteLine(result.StandardOutput);
                    }

                    if (String.IsNullOrEmpty(result.StandardError) == false)
                    {
                        AnsiConsole.WriteLine(result.StandardError);
                    }

                    Environment.Exit(1);
                }

                var vaults = JsonSerializer.Deserialize<Vault[]>(result.StandardOutput);
                if (vaults is null || vaults.Length == 0)
                {
                    AnsiConsoleHelper.DisplayErrorAndContinue("No vaults found.");
                    return;
                }

                var vaultsDictionary = new Dictionary<string, Vault>();
                var vaultOptions = new List<string>();
                foreach (var vault in vaults)
                {
                    vaultsDictionary.Add(vault.GetDisplayName(), vault);
                }

                vaultOptions.AddRange(vaultsDictionary.Keys);
                vaultOptions.Add("Back");
                vaultOptions.Add("Quit");


                var prompt = new SelectionPrompt<string>()
                    .Title("Select your vault:")
                    .PageSize(10)
                    .EnableSearch()
                    .AddChoices(vaultOptions);

                var response = AnsiConsole.Prompt(prompt);

                if (response == "Back")
                {
                    return;
                }
                else if (response == "Quit")
                {
                    Environment.Exit(0);
                }

                var selectedVault = vaultsDictionary[response];
                await selectedVault.SelectItemsAsync(this);
            }
        }
        catch (Exception err)
        {
            // TODO: If the error message contains [ or ] it should be escaped to [[ or ]]
            AnsiConsole.MarkupLine($"[red]Error: {err.Message}[/]");
            Environment.Exit(1);
        }
    }
}

