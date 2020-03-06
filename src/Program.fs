namespace Misc.BgStats

open System
open Microsoft.Extensions.Configuration

open Serilog
open Serilog.Sinks.SystemConsole

open Models

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

    let run =
        logger.Information("asdf")

    [<EntryPoint>]
    let main argv =
        run

        Console.ReadLine() |> ignore
        0