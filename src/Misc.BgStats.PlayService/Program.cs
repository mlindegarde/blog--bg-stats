using System;
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
        private TopshelfExitCode _exitCode;
        #endregion

        #region Start-up / Config
        private Program LoadConfig()
        {
            return this;
        }

        private Program InitLogger()
        {
            return this;
        }

        private Program InitIoC()
        {
            return this;
        }
        #endregion

        #region Main Method
        private Program Run()
        {
            _exitCode =
                HostFactory.Run(
                    hostConfig =>
                    {
                        hostConfig.Service<PlayLogger>(
                            serviceConfig =>
                            {
                                serviceConfig.ConstructUsing(hostSettings => new PlayLogger(hostSettings));
                                serviceConfig.WhenStarted(x => x.Start());
                                serviceConfig.WhenStopped(x => x.Stop());
                            });
                        hostConfig.RunAsLocalSystem();

                        hostConfig.SetServiceName(ServiceName);
                        hostConfig.SetDisplayName(ServiceDisplayName);
                        hostConfig.SetDescription(ServiceDescription);
                    });

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
