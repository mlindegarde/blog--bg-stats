namespace Misc.BgStats.Application

open System
open System.Net
open System.Net.Http
open System.Xml.Linq

open Flurl
open FSharp.Control.Tasks.V2

open Misc.BgStats.Domain.Models

module BoardGameGeekClient =
    let LogInAsync (bggSettings : BoardGameGeekSettings) (client : HttpClient) =
        task {
            use content =
                new FormUrlEncodedContent(
                    dict[
                        "username", bggSettings.Username;
                        "password", bggSettings.Password])

            let! response = client.PostAsync("/login", content)

            if not (response.StatusCode.Equals HttpStatusCode.OK) then
                raise (ApplicationException("Authentication failed"))
        }

    let rec GetAsync (uri : Uri) (client : HttpClient) =
        task {
            let! response = client.GetAsync uri

            if response.StatusCode.Equals HttpStatusCode.OK then
                let! contentAsString = response.Content.ReadAsStringAsync()
                return XDocument.Parse(contentAsString)
            else
                return! GetAsync uri client
        }

    let xn s = XName.Get(s)

    let GetCollectionAsync (bggSettings : BoardGameGeekSettings) (client : HttpClient) =
        task {
            let! collectionXml = 
                client |> GetAsync(
                    "https://www.boardgamegeek.com"
                        .AppendPathSegments("xmlapi2", "collection")
                        .SetQueryParam("username", bggSettings.Username)
                        .SetQueryParam("showprivate", 1)
                        .SetQueryParam("stats", 1)
                        .SetQueryParam("own", 1)
                        .ToUri())

            if isNull collectionXml.Root then
                raise (ApplicationException("Failed to get collection"))

            let collection = {
                OwnerUsername = bggSettings.Username;
                BoardGames = 
                    collectionXml.Root.Elements(xn "item") 
                    |> Seq.filter(fun i -> i.Attribute(xn "subtype").Value = "boardgame") 
                    |> Seq.map(fun e -> {
                        ObjectId = Int32.Parse(e.Attribute(xn "objectid").Value);
                        Name = e.Element(xn "name").Value;
                        YearPublished = Int32.Parse(e.Element(xn "yearpublished").Value)
                    })
                    |> Seq.toList}

            return collection
        }