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

    let runAsync = 
        task {
            let cookieJar = CookieContainer()
            let handler = new HttpClientHandler()
            handler.CookieContainer <- cookieJar
            let client = new HttpClient(handler)
            client.BaseAddress <- Uri("https://www.boardgamegeek.com")

            do! client |> BoardGameGeekClient.LogInAsync (config.BoardGameGeek)
            let! collection = client |> BoardGameGeekClient.GetCollectionAsync (config.BoardGameGeek)

            return ()
        }

    [<EntryPoint>]
    let main argv =
        runAsync |> Async.AwaitTask |> Async.RunSynchronously
        Console.ReadLine() |> ignore
        0