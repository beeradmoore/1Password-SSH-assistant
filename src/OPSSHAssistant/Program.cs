// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.Json;
using OPSSHAssistant.Data;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

var prompt = new SelectionPrompt<string>()
    .Title("This tool will use the 1Password CLI to list accounts, vaults, and items. Are you want to continue?")
    .AddChoices(new[] { "Yes", "Quit" });

var response = AnsiConsole.Prompt(prompt);

if (response == "Quit")
{
    // Handle the quit logic here
    Environment.Exit(0);
}

SelectAccount:

var result = await Cli.Wrap("op")
    .WithArguments("account list --format json --no-color")
    .ExecuteBufferedAsync();

if (result.IsSuccess == false)
{
    // TOOD: Log error
    Environment.Exit(1);
}

var accounts = JsonSerializer.Deserialize<Account[]>(result.StandardOutput);
var accountsDictionary = new Dictionary<string, Account>();
var accountOptions = new List<string>();
foreach (var account in accounts)
{
    accountsDictionary.Add(account.GetDisplayName(), account);
}
accountOptions.AddRange(accountsDictionary.Keys);
accountOptions.Add("Quit");

prompt = new SelectionPrompt<string>()
    .Title("Please select your account?")
    .AddChoices(accountOptions);

response = AnsiConsole.Prompt(prompt);

if (response == "Quit")
{
    // Handle the quit logic here
    Environment.Exit(0);
}

var selectedAccount = accountsDictionary[response];

SelectVault:

result = await Cli.Wrap("op")
    .WithArguments($"vault list --account {selectedAccount.AccountUuid} --format json --no-color")
    .ExecuteBufferedAsync();

if (result.IsSuccess == false)
{
    // TOOD: Log error
    Environment.Exit(1);
}

var vaults = JsonSerializer.Deserialize<Vault[]>(result.StandardOutput);

var vaultsDictionary = new Dictionary<string, Vault>();
var vaultOptions = new List<string>();
foreach (var vault in vaults)
{
    vaultsDictionary.Add(vault.GetDisplayName(), vault);
}
vaultOptions.AddRange(vaultsDictionary.Keys);
vaultOptions.Add("Back");
vaultOptions.Add("Quit");


prompt = new SelectionPrompt<string>()
    .Title("Please select your vault?")
    .AddChoices(vaultOptions);


response = AnsiConsole.Prompt(prompt);

if (response == "Back")
{
    goto SelectAccount;
}
else if (response == "Quit")
{
    // Handle the quit logic here
    Environment.Exit(0);
}



var selectedVault = vaultsDictionary[response];


result = await Cli.Wrap("op")
    .WithArguments($"item list --account {selectedAccount.AccountUuid} --vault {selectedVault.Id} --format json --no-color --categories \"SSH Key\"")
    .ExecuteBufferedAsync();

var items = JsonSerializer.Deserialize<Item[]>(result.StandardOutput);

if (items.Length == 0)
{
    AnsiConsole.MarkupLine("[red]No SSH Key found in the selected vault[/]");
    goto SelectVault;
}



Debugger.Break();



// Ask for the user's favorite fruits


var fruits = AnsiConsole.Prompt(
    new MultiSelectionPrompt<string>()
        .Title("What are your [green]favorite fruits[/]?")
        .NotRequired() // Not required to have a favorite fruit
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
        .InstructionsText(
            "[grey](Press [blue]<space>[/] to toggle a fruit, " + 
            "[green]<enter>[/] to accept)[/]")
        .AddChoiceGroup("Fruit", new[] {
            "Apple", "Apricot", "Avocado",
            "Banana", "Blackcurrant", "Blueberry",
            "Cherry", "Cloudberry", "Coconut",
        })
        .AddChoiceGroup("Vegtables", new[] {
            "Asparagus", "Aubergine", "Artichoke",
            "Broccoli", "Beetroot", "Brussels sprouts",
            "Cabbage", "Carrot", "Cauliflower",
        })
        );

// Write the selected fruits to the terminal
foreach (string fruit in fruits)
{
    AnsiConsole.WriteLine(fruit);
}