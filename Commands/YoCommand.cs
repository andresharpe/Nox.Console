using System.Text.Json;
using System.ComponentModel;

using Spectre.Console.Cli;
using Spectre.Console;

using Microsoft.Extensions.Logging;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using NoxConsole.Data;

namespace NoxConsole.Commands;

public class YoCommand : AsyncCommand<YoCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-l|--language <LANGUAGE>")]
        [Description("The language you want to be greeted in.")]
        public string Language { get; set; }
    }
    
    public const string HelloSeedDataFile = "@./Resources/SeedData/Hello.json";

    private readonly ILogger<YoCommand> _logger;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _dbContext;

    public YoCommand(ILogger<YoCommand> logger, IConfiguration config, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _config = config;
        _dbContext = dbContext;
    }


    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (_dbContext.Hello.Count() == 0)
        {
            await SeedHelloRowsFromJson();
        }

        Hello hello;

        if (settings.Language is null)
        {
            Random rand = new Random();
            int toSkip = rand.Next(1, _dbContext.Hello.Count());
            hello = _dbContext.Hello.OrderBy(h => h.Id).Skip(toSkip).Take(1).FirstOrDefault();
        }
        else
        {
            hello = _dbContext.Hello.Where(h => h.Language == settings.Language).FirstOrDefault();
        }

        if (hello is null)
        {
            AnsiConsole.MarkupLine($"Sorry, I don't speak [{Program.AppAccentColor}]{settings.Language}[/]");

            _logger.LogWarning("'Hello' requested in unknown language {language}", settings.Language);
        }
        else
        { 
            AnsiConsole.MarkupLine($"{hello.HelloPhrase} (in {hello.Language})");

            _logger.LogInformation("{hello}", hello);
        }

        return 0;
    }

    private async Task SeedHelloRowsFromJson()
    {
        // Quick way

        // await _dbContext.AddRangeAsync(JsonSerializer.Deserialize<List<Hello>>(File.ReadAllText(@"./Resources/Hello.json")));
        // await _dbContext.SaveChangesAsync();
        // return;

        // Pretty way..
        await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            var task1 = ctx.AddTask($"[{Program.AppAccentColor}]Importing Data[/]");
            var fileData = JsonSerializer.Deserialize<List<Hello>>(File.ReadAllText(HelloSeedDataFile));
            var rows = fileData.Count();
            var increment = 100f / rows;
            foreach (var row in fileData)
            {
                await _dbContext.AddAsync(row);
                task1.Increment(increment);
                await Task.Delay(10); 
            }
            task1.StopTask();
        });
        await _dbContext.SaveChangesAsync();
    }
}