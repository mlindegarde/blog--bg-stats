namespace Misc.BgStats.AverageScore

open System
open System.Net
open System.Net.Http

open Serilog
open FSharp.Control.Tasks.V2

open Misc.BgStats.AverageScore
open Misc.BgStats.AverageScore.Services

module Program =
    let logger =
        LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger()

    let initClient() =
        let cookieJar = CookieContainer()
        let handler = new HttpClientHandler()

        handler.CookieContainer <- cookieJar

        let client = new HttpClient(handler)

        client.BaseAddress <- Uri ("https://www.boardgamegeek.com")
        client

    let getStatsAsync (objectId : int) =
        task {
            use client = initClient()

            let! details = client |> BoardGameGeekClient.getDetailDataAsync [|objectId|] logger

            let boardGameName = (details.[0].Names |> Array.find (fun n -> n.Type = "primary")).Value

            let maxPlayerCount = details.[0].Maxplayers.Value
            let minPlayerCount = details.[0].Minplayers.Value
            
            let! plays = MongoService.getPlaysForAsync objectId logger

            return plays
                |> List.filter (fun play -> play.Players.Length >= minPlayerCount && play.Players.Length <= maxPlayerCount)
                |> List.map (fun play -> play.Players)
                |> List.groupBy (fun players -> players.Length)
                |> List.sortBy (fun (playerCount, _) -> playerCount)
                |> List.map(
                    fun (groupPlayerCount, groupPlayers) ->
                        let validScores =
                            groupPlayers
                            |> List.collect (fun players -> players |> Array.toList)
                            |> List.map (fun player -> player.Score)
                            |> List.filter (fun score -> score > 0)

                        let avgScore =
                            validScores
                            |> List.averageBy (fun score -> (double score))

                        (boardGameName, groupPlayerCount, validScores.Length, avgScore))
        }

    let runAsync =
        task {
            for objectId in Literals.AllGames do
                let! results = getStatsAsync objectId
                Renderer.displayAverageScore results
        }

    [<EntryPoint>]
    let main argv =
        runAsync
        |> Async.AwaitTask
        |> Async.RunSynchronously
        0
