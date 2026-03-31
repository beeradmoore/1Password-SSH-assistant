using System.Diagnostics;
using SSHAssistantFor1Password.CLI;
using Spectre.Console;
using System.Runtime.InteropServices;
using System.Text;
using SSHAssistantFor1Password.Core;
using SSHAssistantFor1Password.Core.Data;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false &&
    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == false &&
    RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == false)
{
    Console.WriteLine("Error: Unsure where you are running this, but it only supports being run on Windows, macOS, and Linux.");
    Environment.Exit(0);
}

var opManager = new OPManager();
var checkFor1PasswordResult = await opManager.CheckFor1PasswordCLIAsync();
if (checkFor1PasswordResult.Success == false || checkFor1PasswordResult.Data != true)
{
    Console.WriteLine("1Password CLI could not be found. Please ensure it is installed and enabled by following the instructions here, https://developer.1password.com/docs/cli/get-started/");
    Debugger.Break();
    Environment.Exit(0);
}

var prompt = new SelectionPrompt<string>()
    .Title("This tool will use the 1Password CLI to list accounts, vaults, and items. You will be prompted to authorise access multiple times in this process. Are you sure you want to continue?")
    .AddChoices(new[] { "Yes", "Quit" });

var response = AnsiConsole.Prompt(prompt);

if (response == "Quit")
{
    Environment.Exit(0);
}


try
{
    // Create SSH directory if it does not exist.
    if (Directory.Exists(opManager.GetSSHPath()) == false)
    {
        Directory.CreateDirectory(opManager.GetSSHPath());

        // Create SSH config if it does not exist.
        if (File.Exists(opManager.GetSSHConfigPath()))
        {
            File.WriteAllText(opManager.GetSSHConfigPath(), string.Empty);
        }
    }
}
catch (Exception )
{
    AnsiConsoleHelper.DisplayErrorAndContinue($"SSH directory ({opManager.GetSSHPath()}) does not exist. SSH pathing needs to be configured for public key generation to work.");
}

try
{
// Create directory for agent.toml does not exist.
    if (Directory.Exists(opManager.GetAgentTomlDirectory()) == false)
    {
        Directory.CreateDirectory(opManager.GetAgentTomlDirectory());

        // Create agent.toml if it does not exist.
        if (File.Exists(opManager.GetAgentTomlPath()))
        {
            File.WriteAllText(opManager.GetAgentTomlPath(), string.Empty);
        }
    }
}
catch (Exception )
{
    AnsiConsoleHelper.DisplayErrorAndContinue($"Agent.toml file ({opManager.GetAgentTomlPath()}) does not exist. This file needs to be configured for public key generation to work.");
}

try
{
    while (true)
    {
        var accounts = await opManager.LoadAccountsAsync();

        if (accounts.Success == false || accounts.Data == null || accounts.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: Could not list accounts.[/]");
            AnsiConsole.WriteLine(accounts.ErrorMessage);
            Environment.Exit(1);
        }

        var accountsDictionary = new Dictionary<string, Account>();
        var accountOptions = new List<string>();
        foreach (var account in accounts.Data)
        {
            accountsDictionary.Add(account.GetDisplayName(), account);
        }

        accountOptions.AddRange(accountsDictionary.Keys);
        accountOptions.Add("Quit");

        var selectAccountPrompt = new SelectionPrompt<string>()
            .Title("Select your account:")
            .AddChoices(accountOptions);

        var selectAccountResponse = AnsiConsole.Prompt(selectAccountPrompt);

        if (selectAccountResponse == "Quit")
        {
            Environment.Exit(0);
        }

        var selectedAccount = accountsDictionary[selectAccountResponse];

        var vaults = await opManager.LoadVaultsAsync(selectedAccount);
        if (vaults.Success == false || vaults.Data is null || vaults.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: Could not list vaults.[/]");
            AnsiConsole.WriteLine(vaults.ErrorMessage);
            Environment.Exit(1);
        }

        var vaultsDictionary = new Dictionary<string, Vault>();
        var vaultOptions = new List<string>();
        foreach (var vault in vaults.Data)
        {
            vaultsDictionary.Add(vault.GetDisplayName(), vault);
        }
        vaultOptions.AddRange(vaultsDictionary.Keys);
        vaultOptions.Add("Back");
        vaultOptions.Add("Quit");

        while (true)
        {
            var selectVaultPrompt = new SelectionPrompt<string>()
                .Title("Select your vault:")
                .PageSize(10)
                .EnableSearch()
                .AddChoices(vaultOptions);

            var selectVaultResponse = AnsiConsole.Prompt(selectVaultPrompt);

            if (selectVaultResponse == "Back")
            {
                break;
            }
            else if (selectVaultResponse == "Quit")
            {
                Environment.Exit(0);
            }

            var selectedVault = vaultsDictionary[selectVaultResponse];

            var items = await opManager.LoadItemsAsync(selectedAccount, selectedVault);
            if (items.Success == false || items.Data is null)
            {
                AnsiConsole.MarkupLine("[red]Error: Could not list items.[/]");
                AnsiConsole.WriteLine(items.ErrorMessage);
                Environment.Exit(1);
            }

            if (items.Data.Count == 0)
            {
                AnsiConsoleHelper.DisplayErrorAndContinue("No SSH keys.");
                break;
            }

            var itemsDictionary = new Dictionary<string, Item>();
            foreach (var item in items.Data)
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
            foreach (var selectedItem in selectedItems)
            {
                selectedItemObjects.Add(itemsDictionary[selectedItem]);
            }

            // Create SSH directory if it does not exist.
            if (Directory.Exists(opManager.GetSSHPath()) == false)
            {
                Directory.CreateDirectory(opManager.GetSSHPath());

                // Create SSH config if it does not exist.
                if (File.Exists(opManager.GetSSHConfigPath()))
                {
                    File.WriteAllText(opManager.GetSSHConfigPath(), string.Empty);
                }
            }


            // Create directory for agent.toml does not exist.
            if (Directory.Exists(opManager.GetAgentTomlDirectory()) == false)
            {
                Directory.CreateDirectory(opManager.GetAgentTomlDirectory());

                // Create agent.toml if it does not exist.
                if (File.Exists(opManager.GetAgentTomlPath()))
                {
                    File.WriteAllText(opManager.GetAgentTomlPath(), string.Empty);
                }
            }

            var anyPublicKeysNeedExport = await opManager.LoadPublicKeysToExportAsync(selectedAccount, selectedVault, selectedItemObjects);

            if (anyPublicKeysNeedExport.Success == false)
            {
                AnsiConsole.MarkupLine("[red]Error: Could determine public keys to export.[/]");
                AnsiConsole.WriteLine(anyPublicKeysNeedExport.ErrorMessage);
                Environment.Exit(1);
            }

            foreach (var selectedItemObject in selectedItemObjects)
            {
                if (selectedItemObject.NeedsExport)
                {
                    AnsiConsole.MarkupLine($"[green]Export public key for {selectedItemObject.Title} as {Path.GetFileName(selectedItemObject.PublicKeyPath)}[/]");
                }
            }

            var exportPublicKeys = AnsiConsole.Confirm("Export public keys?");
            if (exportPublicKeys)
            {
                foreach (var selectedItemObject in selectedItemObjects)
                {
                    if (selectedItemObject.NeedsExport)
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


            var showManualInstructions = true;

            var updateConfigs = AnsiConsole.Confirm("Would you like SSH Assistant for 1Password to update your SSH config and agent.toml files?");
            if (updateConfigs)
            {
                foreach (var storedItemObject in selectedItemObjects)
                {
                    // Try to prefill host and username
                    if (storedItemObject.Username == Item.DefaultUsername)
                    {
                        if (storedItemObject.Title.Contains("bitbucket", StringComparison.OrdinalIgnoreCase) ||
                            storedItemObject.Title.Contains("github", StringComparison.OrdinalIgnoreCase))
                        {
                            storedItemObject.Username = "git";
                        }
                    }

                    if (storedItemObject.Host == Item.DefaultHost)
                    {
                        if (storedItemObject.Title.Contains("bitbucket", StringComparison.OrdinalIgnoreCase))
                        {
                            storedItemObject.Host = "bitbucket.org";
                        }
                        else if (storedItemObject.Title.Contains("github", StringComparison.OrdinalIgnoreCase))
                        {
                            storedItemObject.Host = "github.com";
                        }
                    }

                    storedItemObject.Host = AnsiConsole.Ask<string>($"SSH host for {storedItemObject.Title}?", storedItemObject.Host);
                    storedItemObject.Username = AnsiConsole.Ask<string>($"SSH username for {storedItemObject.Title}?", storedItemObject.Username);
                    Console.WriteLine();
                }

                var sshConfig = opManager.GenerateUpdatedSSHConfig(selectedAccount, selectedVault, selectedItemObjects);
                var agentToml = opManager.GenerateUpdatedAgentToml(selectedAccount, selectedVault, selectedItemObjects);

                try
                {
                    File.AppendAllText(opManager.GetSSHConfigPath(), sshConfig);
                    File.AppendAllText(opManager.GetAgentTomlPath(), agentToml);
                    showManualInstructions = false;
                    AnsiConsole.MarkupLine($"[green]SSH config has been exported and agent.toml has been updated.[/]");

                }
                catch (Exception err)
                {
                    AnsiConsole.MarkupLine($"[red]Error: Could not append configs automatically. You will need to update them manually. ({err.Message})[/]");
                }
            }

            if (showManualInstructions)
            {
                var sshConfig = opManager.GenerateUpdatedSSHConfig(selectedAccount, selectedVault, selectedItemObjects);
                var agentToml = opManager.GenerateUpdatedAgentToml(selectedAccount, selectedVault, selectedItemObjects);

                Console.WriteLine("\n\n");
                Console.WriteLine("The following config needs to appended to:");
                Console.WriteLine(opManager.GetSSHConfigPath());
                Console.WriteLine();
                Console.WriteLine("IMPORTANT: Update UPDATE_HOST_NAME_HERE to be the host name you want to connect to for that ssh key.");
                Console.WriteLine("IMPORTANT: Update UPDATE_USERNAME_HERE to be the username you want to connect with for that ssh key.");
                Console.WriteLine("\n");
                Console.WriteLine(sshConfig);
                Console.WriteLine("\n\n");

                Console.WriteLine();
                Console.WriteLine("The following config needs to appended to:");
                Console.WriteLine(opManager.GetAgentTomlPath());
                Console.WriteLine("\n\n");
                Console.WriteLine(agentToml);
                Console.WriteLine("\n\n");
            }

            Environment.Exit(0);

        }
    }
}
catch (Exception err)
{
    AnsiConsole.MarkupLine($"[red]Error: {err.Message}[/]");
    Environment.Exit(1);
}
