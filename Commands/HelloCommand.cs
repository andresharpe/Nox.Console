using Spectre.Console.Cli;
using Spectre.Console;
using System.ComponentModel;
 
namespace NoxConsole.Commands;

public class HelloCommand : Command<HelloCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-n|--name <NAME>")]
        [Description("The person or thing to greet.")]
        [DefaultValue("World")]
        public string Name { get; set; } = "World";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine($"Hello [{Program.AppAccentColor}]{settings.Name}[/]!");
        return 0;
    }
}