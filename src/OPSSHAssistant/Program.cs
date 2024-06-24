// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using OPSSHAssistant.Data;
using CliWrap;
using OPSSHAssistant;
using Spectre.Console;

try
{
    var result = await Cli.Wrap("op")
        .WithArguments("--help") // --version does not return a valid exit code, so using --help instead.
        .ExecuteAsync();
}
catch (Exception err)
{
    Debug.WriteLine($"Error: {err.Message}");
    Console.WriteLine("1Password CLI could not be found. Please install it from here, https://developer.1password.com/docs/cli/get-started/");
    Debugger.Break();
    Environment.Exit(0);
}




var prompt = new SelectionPrompt<string>()
    .Title("This tool will use the 1Password CLI to list accounts, vaults, and items. Are you sure you want to continue?")
    .AddChoices(new[] { "Yes", "Quit" });

var response = AnsiConsole.Prompt(prompt);

if (response == "Quit")
{
    Environment.Exit(0);
}


var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var sshDirectory = Path.Combine(homeDirectory, ".ssh");
if (Directory.Exists(sshDirectory) == false)
{
    AnsiConsoleHelper.DisplayErrorAndContinue($"SSH directory ({sshDirectory}) does not exist. SSH pathing needs to be configured for public key generation to work.");
}

await Account.SelectAccountAsync();
