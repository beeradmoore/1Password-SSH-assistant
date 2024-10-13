using System.Diagnostics;
using OPSSHAssistant.CLI;
using Spectre.Console;
using System.Runtime.InteropServices;
using System.Text;
using OPSSHAssistant.Core;
using OPSSHAssistant.Core.Data;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false &&
    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == false &&
    RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == false)
{
    Console.WriteLine("Error: Unsure where you are running this, but it only supports being run on Windows, macOS, and Linux.");
    Environment.Exit(0);
}

var opManager = new OPManager();
if (await opManager.CheckFor1PasswordCLIAsync() == false)
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


if (File.Exists(opManager.GetSSHPath()) == false)
{
    AnsiConsoleHelper.DisplayErrorAndContinue($"SSH directory ({opManager.GetSSHPath()}) does not exist. SSH pathing needs to be configured for public key generation to work.");
}


try
{
    while (true)
    {
        var accounts = await opManager.LoadAccountsAsync();

        if (accounts is null || accounts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: Could not list accounts.[/]");
            AnsiConsole.WriteLine(opManager.LastError);
            Environment.Exit(1);
        }
        
        var accountsDictionary = new Dictionary<string, Account>();
        var accountOptions = new List<string>();
        foreach (var account in accounts)
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
        if (vaults is null || vaults.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: Could not list vaules.[/]");
            AnsiConsole.WriteLine(opManager.LastError);
            Environment.Exit(1);
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
            if (items is null)
            {
                AnsiConsole.MarkupLine("[red]Error: Could not list items.[/]");
                AnsiConsole.WriteLine(opManager.LastError);
                Environment.Exit(1);
            }
            
            if (items.Count == 0)
            {
                AnsiConsoleHelper.DisplayErrorAndContinue("No SSH keys.");
                break;
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

            if (File.Exists(opManager.GetSSHConfigPath()))
            {
                var anyPublicKeysNeedExport = await opManager.LoadPublicKeysToExportAsync(selectedAccount, selectedVault, selectedItemObjects);

                if (anyPublicKeysNeedExport is null)
                {
                    AnsiConsole.MarkupLine("[red]Error: Could determine public keys to export.[/]");
                    AnsiConsole.WriteLine(opManager.LastError);
                    Environment.Exit(1);
                }

                if (anyPublicKeysNeedExport == true)
                {
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
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]No public keys needed exporting. Skipping.[/]");
                }
                
                var sshConfig = opManager.GenerateUpdatedSSHConfig(selectedAccount, selectedVault, selectedItemObjects);
                
                Console.WriteLine("\n\n");
                Console.WriteLine("The following config needs to appended to:");
                Console.WriteLine(opManager.GetSSHConfigPath());
                Console.WriteLine();
                Console.WriteLine("IMPORTANT: Update UPDATE_HOST_NAME_HERE to be the host name you want to connect to for that ssh key.");
                Console.WriteLine("IMPORTANT: Update UPDATE_USERNAME_HERE to be the username you want to connect with for that ssh key.");
                Console.WriteLine("\n");
                Console.WriteLine(sshConfig);
                Console.WriteLine("\n\n");
            }
            else
            {
                AnsiConsole.Markup("[red]SSH directory does not exist. Skipping public key generation.[/]");
            }
            
            var agentToml = opManager.GenerateUpdatedAgentToml(selectedAccount, selectedVault, selectedItemObjects);

            Console.WriteLine();
            Console.WriteLine("The following config needs to appended to:");
            Console.WriteLine(opManager.GetAgentTomlPath());
            Console.WriteLine("\n\n");
            Console.WriteLine(agentToml);
            Console.WriteLine("\n\n");
            
            
                
            Environment.Exit(0);

        }
    }
}
catch (Exception err)
{
    AnsiConsole.MarkupLine($"[red]Error: {err.Message}[/]");
    Environment.Exit(1);
}
