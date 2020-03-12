namespace Misc.BgStats

open System
open System.Net
open System.Net.Http
open Microsoft.Extensions.Configuration

open Serilog

open FSharp.Control.Tasks.V2

open Misc.BgStats.Domain.Models
open Misc.BgStats.Application

module Program =
    let config =
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.local.json", true)
            .Build()
            .Get<Settings>();

    let logger =
        LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger()

    let initClient () =
        let cookieJar = CookieContainer()
        let handler = new HttpClientHandler()

        handler.CookieContainer <- cookieJar

        let client = new HttpClient(handler)

        client.BaseAddress <- config.BoardGameGeek.Url
        client

    let getScore (scoreType : ScoreType) (scores : Score list) =
        (scores |> List.find (fun s -> s.ScoreType = scoreType)).Value

    let displayResults (title : string, limit : int) (rankings : Ranking list) =
        printf "%s%s%s" Environment.NewLine title Environment.NewLine

        let topN = rankings |> List.take (limit)

        let longestName = 
            topN 
            |> List.map (fun r -> r.BoardGame.Name.Length)
            |> List.max

        printf "\u2554"
        [0..3] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..longestName+1] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printfn "\u2557"

        topN
        |> List.iter (fun r ->
            printfn 
                "\u2551%3d \u2502 %-*s \u2502 %6.2f \u2502 %6.2f \u2502 %6.2f \u2502 %6.2f \u2551"
                r.Position
                longestName
                r.BoardGame.Name
                //((char 179).ToString())
                (r.Evaluation.Scores |> List.sumBy (fun s -> s.Value))
                (r.Evaluation.Scores |> getScore ScoreType.Rating)
                (r.Evaluation.Scores |> getScore ScoreType.Plays)
                (r.Evaluation.Scores |> getScore ScoreType.Ownership))

        printf "\u255A"
        [0..3] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..longestName+1] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printfn "\u255D"

    let displayTopNGames (limit : int) = displayResults (sprintf "Top %d Games" limit, limit)
    let displayTop25Games = 25 |> displayTopNGames

    let displayNGamesToPlay (limit : int) = displayResults(sprintf "Top %d Games to Play" limit, limit)
    let display15GamesToPlay = 15 |> displayNGamesToPlay

    let displayNGamesToSellOrTrade (limit : int) = displayResults (sprintf "Top %d Games to Sell / Trade" limit, limit)
    let display15GamesToSellOrTrade = 15 |> displayNGamesToSellOrTrade

    let runAsync = 
        task {
            use client = initClient()

            let! collection = client |> BoardGameGeekClient.getCollectionAsync config.BoardGameGeek logger
            let evaluations = collection |> Evaluator.evaluate

            evaluations |> Ranker.Top25AllTime |> displayTop25Games
            evaluations |> Ranker.GamesToPlay |> display15GamesToPlay
            evaluations |> Ranker.GamesToSellOrTrade |> display15GamesToSellOrTrade

            printf "%sPress ENTER to exit: " Environment.NewLine
            Console.ReadLine() |> ignore
        }

    [<EntryPoint>]
    let main argv =
        runAsync 
        |> Async.AwaitTask 
        |> Async.RunSynchronously
        0