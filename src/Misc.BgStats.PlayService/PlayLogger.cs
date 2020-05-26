using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Misc.BgStats.PlayService.Config;
using Misc.BgStats.PlayService.Model;
using Misc.BgStats.PlayService.Services;
using Serilog;

namespace Misc.BgStats.PlayService
{
    public class PlayLogger
    {
        #region Constants
        private const int MaxPlaysPerPage = 100;
        #endregion

        #region Member Variables
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private readonly ProgramConfig _config;
        private readonly MongoService _mongoSvc;
        private readonly BoardGameGeekService _bggSvc;
        private readonly ILogger _logger;

        private Task _serviceTask;
        private CancellationToken _cancellationToken;
        #endregion

        #region Constructor
        public PlayLogger(ProgramConfig config, MongoService mongoSvc, BoardGameGeekService bggSvc, ILogger logger)
        {
            _config = config;
            _mongoSvc = mongoSvc;
            _bggSvc = bggSvc;
            _logger = logger;
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
                _serviceTask.Wait(CancellationToken.None);
        }
        #endregion

        #region Main Execution Loop
        private async Task RunAsync()
        {
            _logger.Verbose("Running {ClassName}", nameof(PlayLogger));

            bool error = false;

            try
            {
                List<BoardGame> boardGames = await _mongoSvc.GetBoardListAsync(_cancellationToken);

                _logger.Debug("Will log plays for {GameCount} games:", boardGames.Count);
                boardGames.ForEach(bg => _logger.Debug("\t- {GameName} - {GameId}", bg.Name, bg.ObjectId));

                while (!(_cancellationToken.IsCancellationRequested || error))
                {
                    foreach (BoardGame bg in boardGames)
                    {
                        try
                        {
                            _logger.Information("Starting play logging for {BoardGameName}", bg.Name);
                            BoardGameStatus status = await _mongoSvc.GetBoardGameStatusAsync(bg.ObjectId, _cancellationToken);

                            if (status?.ImportSuccessful == true)
                                await GetMostRecentPlaysAsync(bg, status);
                            else
                                await GetAllPlaysAsync(bg);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Failed to get plays for {GameName}", bg.Name);
                            error = true;
                        }
                    }

                    _logger.Information("Finished processing list, will update in {Minutes} minutes", 30);
                    await Task.Delay(TimeSpan.FromMinutes(30), _cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Something went terribly wrong: {Message}", ex.Message);
            }
        }
        #endregion

        #region Utility Methods
        private async Task GetMostRecentPlaysAsync(BoardGame boardGame, BoardGameStatus status)
        {
            if (status.LastUpdated.Date == DateTime.Now.Date)
            {
                _logger.Information("Already logged plays for {BoardGameName} today", boardGame.Name);
                return;
            }

            DateTime maxDate = DateTime.Now;
            DateTime minDate = maxDate.AddDays(-7);
            int page = 1;

            _logger.Information(
                "Getting plays between {MinDate} and {MaxDate} for {BoardGameName}", 
                boardGame.Name, 
                minDate.Date, 
                maxDate.Date);

            GetPlaysResult result;

            do
            {
                result = await _bggSvc.GetPlaysAsync(boardGame.ObjectId, minDate, maxDate, page);

                if (result.TooManyRequests)
                {
                    _logger.Error("Too many requests, will wait to resume...");
                    await Task.Delay(TimeSpan.FromSeconds(60), _cancellationToken);
                    continue;
                }

                if (!result.WasSuccessful)
                {
                    _logger.Error("Failed to get plays, not sure what to do now...");
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    continue;
                }

                _logger.Information(
                    "Successfully downloaded page {CurrentPage} of {TotalPages}", 
                    result.Page, 
                    Math.Ceiling(result.TotalCount/100d));

                await _mongoSvc.UpsertPlaysAsync(result.Plays, _cancellationToken);
                page++;

            } while (!result.WasSuccessful || result.Plays.Count == MaxPlaysPerPage);

            status.LastUpdated = DateTime.Now;
            await _mongoSvc.UpsertBoardGameStatusAsync(status, _cancellationToken);

        }

        private async Task GetAllPlaysAsync(BoardGame boardGame)
        {
            int page = 1;
            GetPlaysResult result;

            _logger.Information("Removing any existing plays for {BoardGameName}", boardGame.Name);
            await _mongoSvc.DeletePlaysFor(boardGame.ObjectId, _cancellationToken);

            _logger.Information("Starting play import for {BoardGameName}", boardGame.Name);

            do
            {
                result = await _bggSvc.GetPlaysAsync(boardGame.ObjectId, page);

                if (result.TooManyRequests)
                {
                    _logger.Error("Too many requests, will wait to resume...");
                    await Task.Delay(TimeSpan.FromSeconds(60), _cancellationToken);
                    continue;
                }

                if (!result.WasSuccessful)
                {
                    _logger.Error("Failed to get plays, not sure what to do now...");
                    continue;
                }

                _logger.Information(
                    "Successfully downloaded page {CurrentPage} of {TotalPages}",
                    result.Page,
                    Math.Ceiling(result.TotalCount / 100d));

                await _mongoSvc.InsertPlaysAsync(result.Plays, _cancellationToken);
                page++;

            } while (!result.WasSuccessful || result.Plays.Count == MaxPlaysPerPage);

            await _mongoSvc.UpsertBoardGameStatusAsync(
                new BoardGameStatus
                {
                    ObjectId = boardGame.ObjectId,
                    BoardGameName = boardGame.Name,
                    ImportSuccessful = true,
                    LastUpdated = DateTime.Now
                },
                _cancellationToken);
        }
        #endregion
    }
}
