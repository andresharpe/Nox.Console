using System.Text.Json;
using System.ComponentModel;

using Spectre.Console.Cli;
using Spectre.Console;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace NoxConsole.Commands;

public class IpCommand : AsyncCommand<IpCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-a|--address <IP_ADDRESS>")]
        [Description("The public IP address to show information about.")]
        public string IpAddress { get; set; }

        [CommandOption("-j|--json")]
        [Description("Output the result in json.")]
        [DefaultValue(false)]
        public bool AsJson { get; set; }
    }

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IpCommand> _logger;
    private readonly IConfiguration _config;

    public IpCommand(IHttpClientFactory httpClientFactory, ILogger<IpCommand> logger, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var url = (settings.IpAddress is null) ? $"https://ipinfo.io/geo" : $"https://ipinfo.io/{settings.IpAddress}/geo";

        _logger.LogInformation("Making a call to {url}", url);

        var request = new HttpRequestMessage(HttpMethod.Get, url );
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStreamAsync();

            if(settings.AsJson)
            {
                var json = await (new StreamReader(content)).ReadToEndAsync();
                AnsiConsole.WriteLine(json);
                return 0;
            }

            var info = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(content);
            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");
            foreach(var kvp in info)
            {
                table.AddRow(kvp.Key, $"[{Program.AppAccentColor}]{kvp.Value}[/]");
            }       
            AnsiConsole.Write(table);
            return 0;
        }

        _logger.LogError("Status {statuscode} returned on {url}", response.StatusCode, url);
        
        AnsiConsole.MarkupLine($"Oops. Something went wrong (StatusCode: {response.StatusCode})");

        return 1;
    }
}