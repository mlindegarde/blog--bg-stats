using System;
using System.Threading;
using System.Threading.Tasks;
using Topshelf.Runtime;

namespace Misc.BgStats.PlayService
{
    public class PlayLogger
    {
        #region Member Variables
        private readonly HostSettings _hostSettings;

        private Task _serviceTask;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken;
        #endregion

        #region Constructor
        public PlayLogger(HostSettings hostSettings)
        {
            _hostSettings = hostSettings;
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
            while (!_cancellationToken.IsCancellationRequested)
            {



                await Task.Delay(TimeSpan.FromMinutes(30), _cancellationToken);
            }
        }
        #endregion
    }
}
