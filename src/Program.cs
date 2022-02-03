/// A console application for building CLI applications with:-
/// https://github.com/andresharpe/Nox.Console

using NoxConsole.Commands;
using NoxConsole.Data;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.EntityFrameworkCore;

using Spectre.Console.Cli;

using Serilog;

namespace NoxConsole;

class Program
{

    public const string AppAccentColor = "aquamarine3";

    static int Main(string[] args)
    {

        var hostBuilder = CreateHostBuilder(args);
        var registrar = new TypeRegistrar(hostBuilder);
        var app = new CommandApp(registrar);

        app.SetDefaultCommand<HelloCommand>();

        app.Configure(config =>
        {
            #if DEBUG
                config.PropagateExceptions();
                config.ValidateExamples();
            #endif
            
            config.AddCommand<HelloCommand>("hello")
                .WithDescription("Say hello to anyone.")
                .WithExample(new[] { "hello", "--name", "Anakin" });            
                
            config.AddCommand<IpCommand>("ip")
                .WithDescription("Display information about an IP address.")
                .WithExample(new[] { "ip" })
                .WithExample(new[] { "ip", "--address", "8.8.8.8" })
                .WithExample(new[] { "ip", "--json" });

            config.AddCommand<YoCommand>("yo")
                .WithDescription("Say hello in multiple languages.")
                .WithExample(new[] { "yo", "--language", "Afrikaans" });            
                

        });

        return app.Run(args);    
    }

    public static IHostBuilder CreateHostBuilder(string[] args) 
    {
        // App Configuration
        
        var configuration = ConfigurationHelper.GetApplicationConfiguration(args);

        // Logger

        ILogger logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Logger = logger;
        
        // HostBuilder

        var hostBuilder = Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClient();
                services.AddSingleton<IConfiguration>(configuration);
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("ApplicationDbContext")));
                services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
            })
            .UseSerilog();

        return hostBuilder;
    }

}

