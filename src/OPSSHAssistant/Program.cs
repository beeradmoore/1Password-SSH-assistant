// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.Json;
using OPSSHAssistant.Data;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

var prompt = new SelectionPrompt<string>()
    .Title("This tool will use the 1Password CLI to list accounts. It will not scan for vaults or items until you explicitly select them. Are you want to continue?")
    .AddChoices(new[] { "Yes", "Quit" });

var response = AnsiConsole.Prompt(prompt);

if (response == "Quit")
{
    // Handle the quit logic here
    Environment.Exit(0);
}


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


result = await Cli.Wrap("op")
    .WithArguments($"vault list --account {selectedAccount.AccountUuid} --format json --no-color")
    .ExecuteBufferedAsync();

if (result.IsSuccess == false)
{
    // TOOD: Log error
    Environment.Exit(1);
}

Console.WriteLine(result);

Debugger.Break();
/*
var processStartInfo = new ProcessStartInfo();
processStartInfo.FileName = "op";
processStartInfo.Arguments = "account list --format json";
processStartInfo.RedirectStandardOutput = true;
var process = Process.Start(processStartInfo);
process.WaitForExit();
var output = process.StandardOutput.ReadToEnd();
Debugger.Break();
*/


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