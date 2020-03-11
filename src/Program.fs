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

    let displayResults (title : string, limit : int) (rankings : Ranking list) =
        printf "%s%s%s" Environment.NewLine title Environment.NewLine

        rankings
        |> List.take(limit)
        |> List.iter(fun r ->
            printfn 
                "%3d: %s - %.2f"
                r.Position
                r.BoardGame.Name
                (r.Evaluation.Scores |> List.sumBy (fun s -> s.Value)))


    let runAsync = 
        task {
            use client = initClient()

            let! collection = client |> BoardGameGeekClient.getCollectionAsync config.BoardGameGeek logger
            let evaluations = collection |> Evaluator.evaluate

            evaluations |> Ranker.RankByScore |> displayResults ("Top 25 Games", 25)

            printf "%sPress ENTER to exit: " Environment.NewLine
            Console.ReadLine() |> ignore
        }

    [<EntryPoint>]
    let main argv =
        runAsync 
        |> Async.AwaitTask 
        |> Async.RunSynchronously
        0