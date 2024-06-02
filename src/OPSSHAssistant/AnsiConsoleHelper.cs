using Spectre.Console;

namespace OPSSHAssistant;

public static class AnsiConsoleHelper
{
	public static void DisplayErrorAndContinue(string errorMessage)
	{
		AnsiConsole.MarkupLine($"[red]Error: {errorMessage}[/]");
		AnsiConsole.Prompt(new TextPrompt<string>("Press return to continue...").AllowEmpty());
		AnsiConsole.Clear();
	}
}