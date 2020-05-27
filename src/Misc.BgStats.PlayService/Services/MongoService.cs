using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Misc.BgStats.PlayService.Model;
using MongoDB.Driver;
using Serilog;

namespace Misc.BgStats.PlayService.Services
{
    public class MongoService
    {
        #region Constants
        private const string Database = "board-game-stats";
        private const string BoardGameCollection = "board-games";
        private const string BoardGameStatusCollection = "board-game-status";
        private const string PlayCollection = "plays";
        #endregion

        #region Member Variables
        private readonly MongoClient _client;
        private readonly ILogger _logger;
        #endregion

        #region Constructor
        public MongoService(ILogger logger)
        {
            _client = new MongoClient();
            _logger = logger;
        }
        #endregion

        #region Methods
        public async Task<List<BoardGame>> GetBoardListAsync(CancellationToken cancellationToken)
        {
            IMongoDatabase database = _client.GetDatabase(Database);
            IMongoCollection<BoardGame> collection = database.GetCollection<BoardGame>(BoardGameCollection);

            return await collection.Find(x => true).ToListAsync(cancellationToken);
        }

        public async Task<long> GetPlayCountAsync(int id, CancellationToken cancellationToken)
        {
            IMongoDatabase database = _client.GetDatabase(Database);
            IMongoCollection<Play> collection = database.GetCollection<Play>(PlayCollection);

            return await collection.CountDocumentsAsync(x => x.ObjectId == id, new CountOptions(), cancellationToken);
        }

        public async Task InsertPlaysAsync(BoardGame boardGame, List<Play> plays, CancellationToken cancellationToken)
        {
            if (plays?.Any() != true)
            {
                _logger.Warning("Attempted to log an empty play list");
                return;
            }

            IMongoDatabase database = _client.GetDatabase(Database);
            IMongoCollection<Play> collection = database.GetCollection<Play>(PlayCollection);

            _logger.Verbose("Inserting {Count} plays for ObjectId {ObjectID}", plays.Count, plays[0].ObjectId);

            foreach (Play play in plays)
            {
                try
                {
                    ReplaceOneResult result = 
                        await collection.ReplaceOneAsync(
                            x => x.Id == play.Id, play,
                            new ReplaceOptions {IsUpsert = true},
                            cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.Warning("Aborting insert, the operation has been canceled");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex,
                        "Failed to insert play {PlayId} for {GameName}: {ErrorMessage}",
                        play.Id,
                        boardGame.Name,
                        ex.Message);
                }
            }
        }

        public async Task<UpsertPlaysResult> UpsertPlaysAsync(BoardGame boardGame, List<Play> plays, CancellationToken cancellationToken)
        {
            UpsertPlaysResult upsertResult = new UpsertPlaysResult();
            
            if (plays?.Any() != true)
            {
                _logger.Warning("Attempted to log an empty play list");
                return upsertResult;
            }

            IMongoDatabase database = _client.GetDatabase(Database);
            IMongoCollection<Play> collection = database.GetCollection<Play>(PlayCollection);

            _logger.Verbose("Upserting {Count} plays for ObjectId {ObjectID}", plays.Count, plays[0].ObjectId);

            foreach (Play play in plays)
            {
                try
                {
                    ReplaceOneResult result =
                        await collection.ReplaceOneAsync(
                            x => x.Id == play.Id, play,
                            new ReplaceOptions {IsUpsert = true},
                            cancellationToken);

                    if (result.ModifiedCount == 0 && result.MatchedCount == 0 && result.UpsertedId != null)
                        upsertResult.InsertedCount++;

                    upsertResult.MatchedCount += result.MatchedCount;
                    upsertResult.ModifiedCount += result.ModifiedCount;
                    upsertResult.WasSuccessful = true;
                }
                catch (OperationCanceledException)
                {
                    _logger.Warning("Aborting upsert, the operation has been canceled");
                }
                catch (Exception ex)
                {
                    _logger.Error( ex,
                        "Failed to upsert play {PlayId} for {GameName}: {ErrorMessage}",
                        play.Id,
                        boardGame.Name,
                        ex.Message);
                }
            }

            return upsertResult;
        }

        public async Task DeletePlaysFor(int id, CancellationToken cancellationToken)
        {
            IMongoDatabase database = _client.GetDatabase(Database);
            IMongoCollection<Play> collection = database.GetCollection<Play>(PlayCollection);

            _logger.Verbose("Removing all plays for {ObjectId}", id);
            await collection.DeleteManyAsync(x => x.ObjectId == id, cancellationToken).ContinueWith(t => { }, CancellationToken.None);
        }

        public async Task<BoardGameStatus> GetBoardGameStatusAsync(int id, CancellationToken cancellationToken)
        {
            IMongoDatabase database = _client.GetDatabase(Database);
            IMongoCollection<BoardGameStatus> collection = database.GetCollection<BoardGameStatus>(BoardGameStatusCollection);

            try
            {
                return await collection.Find(x => x.ObjectId == id).FirstOrDefaultAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        public async Task UpsertBoardGameStatusAsync(BoardGameStatus status, CancellationToken cancellationToken)
        {
            if (status == null)
            {
                _logger.Warning("Attempted to upsert a null status");
                return;
            }

            IMongoDatabase database = _client.GetDatabase(Database);
            IMongoCollection<BoardGameStatus> collection = database.GetCollection<BoardGameStatus>(BoardGameStatusCollection);

            _logger.Verbose("Upserting status for {ObjectId}", status.ObjectId);
            await collection.ReplaceOneAsync(
                x => x.ObjectId == status.ObjectId, 
                status, 
                new ReplaceOptions { IsUpsert = true },
                cancellationToken).ContinueWith(t => { }, CancellationToken.None);
        }
        #endregion
    }
}
