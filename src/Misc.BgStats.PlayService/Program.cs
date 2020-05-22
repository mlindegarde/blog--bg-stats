using System;
using Lamar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Misc.BgStats.PlayService.Config;
using Serilog;
using Topshelf;

namespace Misc.BgStats.PlayService
{
    public class Program
    {
        #region Constants
        private const string ServiceName = "BgStats.PlayService";
        private const string ServiceDisplayName = "BG Stats Play Service";
        private const string ServiceDescription = "Service used to copy BGG plays locally";
        #endregion

        #region Member Variables
        private ProgramConfig _config;
        private ILogger _logger;
        private IContainer _container;
        private TopshelfExitCode _exitCode;
        #endregion

        #region Start-up / Config
        private Program LoadConfig()
        {
            _config =
                new ConfigurationBuilder()
                    .AddJsonFile("programsettings.json", optional: false)
                    .AddJsonFile("programsettings.local.json", optional: false)
                    .AddEnvironmentVariables()
                    .Build()
                    .Get<ProgramConfig>();

            return this;
        }

        private Program InitLogger()
        {
            _logger =
                new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Console()
                    .CreateLogger();
                    
            return this;
        }

        private Program InitIoC()
        {
            _container =
                new Container(
                    registry =>
                    {
                        registry.Scan(
                            scanner =>
                            {
                                scanner.AssembliesAndExecutablesFromApplicationBaseDirectory(a => a.FullName.StartsWith("Misc.BgStats"));
                                scanner.WithDefaultConventions();
                            });

                        registry.AddSingleton(_config);
                        registry.AddSingleton(_logger);
                    });
            return this;
        }
        #endregion

        #region Main Method
        private Program Run()
        {
            _logger.Information("Starting TopShelf host...");

            _exitCode =
                HostFactory.Run(
                    hostConfig =>
                    {
                        hostConfig.Service<PlayLogger>(
                            serviceConfig =>
                            {
                                serviceConfig.ConstructUsing(() => _container.GetInstance<PlayLogger>());
                                serviceConfig.WhenStarted(x => x.Start());
                                serviceConfig.WhenStopped(x => x.Stop());
                            });
                        hostConfig.RunAsLocalSystem();

                        hostConfig.SetServiceName(ServiceName);
                        hostConfig.SetDisplayName(ServiceDisplayName);
                        hostConfig.SetDescription(ServiceDescription);
                    });

            _logger.Information("TopShelf host completed");
            return this;
        }
        #endregion

        #region Shutdown / Clean-up
        private void Shutdown()
        {
            Environment.ExitCode = (int)_exitCode;
        }
        #endregion

        #region Entrypoint
        static void Main()
        {
            Program program = new Program();

            program
                .LoadConfig()
                .InitLogger()
                .InitIoC()
                .Run()
                .Shutdown();
        }
        #endregion
    }
}
