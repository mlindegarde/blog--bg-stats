namespace Misc.BgStats.AverageScore.Services

open MongoDB.Driver
open Serilog

open Misc.BgStats.AverageScore.Domain.Models

open FSharp.Control.Tasks.V2

module MongoService =
    [<Literal>]
    let DatabaseName : string = "board-game-stats"

    [<Literal>]
    let PlayCollection : string = "plays"

    let client : MongoClient = MongoClient()

    let getPlaysForAsync (objectId : int) (logger : ILogger) =
        task {
            let database : IMongoDatabase = client.GetDatabase(DatabaseName)
            let collection : IMongoCollection<Play> = database.GetCollection<Play>(PlayCollection)

            let! cursor = collection.FindAsync (fun x -> x.ObjectId = objectId)
            let! plays = cursor.ToListAsync()

            return plays |> List.ofSeq
        }
