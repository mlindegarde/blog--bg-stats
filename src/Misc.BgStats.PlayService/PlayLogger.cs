using System;
using System.Threading;
using System.Threading.Tasks;
using Misc.BgStats.PlayService.Config;
using Serilog;

namespace Misc.BgStats.PlayService
{
    public class PlayLogger
    {
        #region Member Variables
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly ProgramConfig _config;
        private readonly ILogger _logger;

        private Task _serviceTask;
        private CancellationToken _cancellationToken;
        #endregion

        #region Constructor
        public PlayLogger(ProgramConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;

            _logger.Debug("Will log plays for {GameCount} games:", _config.BoardGames.Count);

            _config.BoardGames
                .ForEach(bg => _logger.Debug("\t- {GameName} - {GameId}", bg.Name, bg.Id));
        }
        #endregion

        #region Service Management
        public void Start()
        {
            _cancellationToken = _tokenSource.Token;
            _serviceTask = StartAsync();
        }

        public async Task StartAsync()
        {
            _logger.Verbose("Staring {ClassName}", nameof(PlayLogger));

            await RunAsync();
        }

        public void Stop()
        {
            _tokenSource.Cancel();

            if(_serviceTask?.IsCompleted == false)
                _serviceTask.Wait();
        }
        #endregion

        #region Main Execution Loop
        private async Task RunAsync()
        {
            _logger.Verbose("Running {ClassName}", nameof(PlayLogger));
            while (!_cancellationToken.IsCancellationRequested)
            {



                await Task.Delay(TimeSpan.FromMinutes(30), _cancellationToken);
            }
        }
        #endregion
    }
}
