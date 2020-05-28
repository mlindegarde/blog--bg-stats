namespace Misc.BgStats.AverageScore.Services

open System
open System.Net
open System.Net.Http
open System.Xml.Linq

open Flurl
open FSharp.Control.Tasks.V2
open FSharp.Data
open Serilog


module BoardGameGeekClient =
    type BggDetails = XmlProvider<"Samples/details-sample.xml">

    let rec private getAsStringAsync (uri : Uri) (delayRequest : bool) (logger : ILogger) (client : HttpClient) =
        task {
            // I always want a delay of some sort, if the server gives me response telling me there were
            // to many requests, then make the delay longer.
            let longDelay = 61.0
            let shortDelay = 0.33

            let start = DateTime.Now.Ticks
            let! response = client.GetAsync uri
            let runTime = (TimeSpan.FromTicks (DateTime.Now.Ticks - start)).TotalMilliseconds

            logger.Verbose ("Recieved a {Response} after {Milliseconds}", response.StatusCode, runTime)

            return!
                if response.StatusCode = HttpStatusCode.OK then
                    response.Content.ReadAsStringAsync()
                elif response.StatusCode = HttpStatusCode.TooManyRequests then
                    logger.Warning ("To many requests, delaying for {Delay} seconds", longDelay)
                    getAsStringAsync uri true logger client
                else
                    getAsStringAsync uri false logger client
        }

    let public getDetailDataAsync (objectIds : int[]) (logger : ILogger) (client : HttpClient) =
        task {
            let! detailsXml = 
                client |> getAsStringAsync (
                    "https://www.boardgamegeek.com"
                        .AppendPathSegment("xmlapi2")
                        .AppendPathSegment("thing")
                        .SetQueryParam("id", String.Join(",", objectIds))
                        .SetQueryParam("stats", 1)
                        .ToUri())
                    false
                    logger

            return (BggDetails.Parse (detailsXml)).Items |> Seq.toList
        }