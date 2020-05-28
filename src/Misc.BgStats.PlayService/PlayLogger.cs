using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Misc.BgStats.PlayService.Config;
using Misc.BgStats.PlayService.Model;
using Misc.BgStats.PlayService.Services;
using MongoDB.Bson;
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

                _logger.Information("Will log plays for {GameCount} games:", boardGames.Count);
                boardGames.ForEach(bg => _logger.Information("\t- {GameName} - {GameId}", bg.Name, bg.ObjectId));
                _logger.Information($"Beginning update loop...{Environment.NewLine}");

                while (!(_cancellationToken.IsCancellationRequested || error))
                {
                    foreach (BoardGame bg in boardGames)
                    {
                        try
                        {
                            if (!_cancellationToken.IsCancellationRequested)
                            {
                                _logger.Information("Starting play logging for {GameName}", bg.Name);
                                BoardGameStatus status = await _mongoSvc.GetBoardGameStatusAsync(bg.ObjectId, _cancellationToken);

                                if (status?.ImportSuccessful == true)
                                    await GetMostRecentPlaysAsync(bg, status);
                                else
                                    await GetAllPlaysAsync(bg);

                                _logger.Information($"Completed updating plays for {{GameName}}{Environment.NewLine}", bg.Name);
                            }
                            else
                            {
                                _logger.Information("Skipping {GameName}, service is shutting down", bg.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Failed to get plays for {GameName}", bg.Name);
                            error = true;
                        }
                    }

                    if (!_cancellationToken.IsCancellationRequested)
                    {
                        _logger.Information("Finished processing list, will update in {Minutes} minutes", _config.UpdateDelayInMinutes);
                        await Task
                            .Delay(TimeSpan.FromMinutes(_config.UpdateDelayInMinutes), _cancellationToken)
                            .ContinueWith(t => { }, CancellationToken.None);
                    }
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
            if (_config.OnlyUpdateOncePerDay && status.LastUpdated.Date == DateTime.Now.Date)
            {
                _logger.Information("Already logged plays for {BoardGameName} today", boardGame.Name);
                return;
            }

            DateTime maxDate = DateTime.Now;
            DateTime minDate = maxDate.AddDays(-_config.IncrementalSpanInDays);
            int page = 1;

            _logger.Information(
                "Getting plays between {MinDate} and {MaxDate} ({DayCount} days) for {BoardGameName}",
                minDate.Date.ToShortDateString(), 
                maxDate.Date.ToShortDateString(),
                maxDate.Subtract(minDate).TotalDays,
                boardGame.Name);

            GetPlaysResult result;

            do
            {
                result = await _bggSvc.GetPlaysAsync(boardGame.ObjectId, minDate, maxDate, page);

                if (result.TooManyRequests)
                {
                    _logger.Warning("Too many requests, will wait to resume...");
                    await Task.Delay(TimeSpan.FromSeconds(60), _cancellationToken).ContinueWith(t => { }, CancellationToken.None);
                    continue;
                }

                if (!result.WasSuccessful)
                {
                    _logger.Error("Failed to get plays, not sure what to do now...");
                    await Task.Delay(TimeSpan.FromSeconds(3), _cancellationToken).ContinueWith(t => { }, CancellationToken.None);
                    continue;
                }

                _logger.Information(
                    "Successfully downloaded page {CurrentPage} of {TotalPages}", 
                    result.Page, 
                    Math.Ceiling(result.TotalCount/100d));

                UpsertPlaysResult upsertResult = await _mongoSvc.UpsertPlaysAsync(boardGame, result.Plays, _cancellationToken);

                if(!upsertResult.WasSuccessful)
                    _logger.Error("Failed to completed the upsert successfully");

                _logger.Information(
                    "Upsert results: Matched {MatchedCount} - Modified {ModifiedCount} - Inserted {InsertedCount}",
                    upsertResult.MatchedCount,
                    upsertResult.ModifiedCount,
                    upsertResult.InsertedCount);

                page++;

            } while ((!result.WasSuccessful || result.Plays.Count == MaxPlaysPerPage) && !_cancellationToken.IsCancellationRequested);

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

                await _mongoSvc.InsertPlaysAsync(boardGame, result.Plays, _cancellationToken);
                page++;

            } while (!result.WasSuccessful || result.Plays.Count == MaxPlaysPerPage);

            await _mongoSvc.UpsertBoardGameStatusAsync(
                new BoardGameStatus
                {
                    Id = ObjectId.GenerateNewId(),
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
