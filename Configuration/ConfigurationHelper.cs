using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace NoxConsole.Data
{
    internal class ConfigurationHelper
    {
        public static IConfiguration GetApplicationConfiguration(string[] args)
        {
            string pathToContentRoot = Directory.GetCurrentDirectory();
            string json = Path.Combine(pathToContentRoot, "appsettings.json");

            if (!File.Exists(json))
            {
                string pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                pathToContentRoot = Path.GetDirectoryName(pathToExe);
            }

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(pathToContentRoot)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
            
            return configuration;

        }
    }
}
