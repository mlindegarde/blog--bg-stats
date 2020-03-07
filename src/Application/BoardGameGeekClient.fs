namespace Misc.BgStats.Application

open System
open System.Net
open System.Net.Http
open System.Xml.Linq

open Flurl
open FSharp.Control.Tasks.V2
open FSharp.Data

open Misc.BgStats.Domain.Models

module BoardGameGeekClient =
    type BggCollection = XmlProvider<"Samples/collection-sample.xml">
    type BggDetails = XmlProvider<"Samples/details-sample.xml">

    let logInAsync (bggSettings : BoardGameGeekSettings) (client : HttpClient) =
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

    let rec getAsStringAsync (uri : Uri) (client : HttpClient) =
        task {
            let! response = client.GetAsync uri

            if response.StatusCode.Equals HttpStatusCode.OK then
                return! response.Content.ReadAsStringAsync()
            else
                return! getAsStringAsync uri client
        }

    let rec getAsXDocumentAsync (uri : Uri) (client : HttpClient) =
        task {
            let! response = client.GetAsync uri

            if response.StatusCode.Equals HttpStatusCode.OK then
                let! contentAsString = response.Content.ReadAsStringAsync()
                return XDocument.Parse(contentAsString)
            else
                return! getAsXDocumentAsync uri client
        }

    let xn s = XName.Get(s)

    let getDetailsAsync (objectIds : int[]) (client : HttpClient) =
        task {
            let! detailsXml = 
                client |> getAsStringAsync(
                    "https://www.boardgamegeek.com"
                       .AppendPathSegment("xmlapi2")
                        .AppendPathSegment("thing")
                        .SetQueryParam("id", String.Join(",", objectIds))
                        .SetQueryParam("stats", 1)
                        .ToUri())
                
            let bggDetails = BggDetails.Parse(detailsXml)

            return 
                bggDetails.Items
                |> Seq.map(
                    fun i -> {
                        ObjectId = i.Id;
                        MaximumPlayers = i.Maxplayers.Value;
                        MinimumPlayers = i.Minplayers.Value;
                    })
                |> Seq.toList
        }

    let toBoardGame (item : BggCollection.Item) (details : Details) = {
        ObjectId = item.Objectid;
        Name = item.Name.Value;
        YearPublished = item.Yearpublished;
        Type = item.Subtype;

        AcquisitionDate = item.Privateinfo |> Option.bind(fun x -> Some(x.Acquisitiondate));

        MyRating = 
            item.Stats.Rating.Value.String 
            |> Option.bind(fun x -> if x <> "N/A" then Some(Decimal.Parse(x)) else None);

        AverageRating = item.Stats.Rating.Average.Value;
        GeekRating = item.Stats.Rating.Bayesaverage.Value;
        NumberOfRatings = item.Stats.Rating.Usersrated.Value;

        OverallRank = 
            item.Stats.Rating.Ranks 
            |> Seq.find(fun r -> r.Name = "boardgame")
            |> (fun x -> if x.Value.Value <> "Not Ranked" then Int32.Parse(x.Value.Value) else 0);

        CategoryRank = 
            match item.Stats.Rating.Ranks |> Seq.tryFind (fun r -> r.Type = "family") with
            | Some(r) -> if r.Value.Value <> "Not Ranked" then Int32.Parse(r.Value.Value) else 0
            | None -> 0;

        IsOwned = item.Status.Own = 1;
        WasOwned = item.Status.Prevowned = 1;
        DoesWant = item.Status.Want = 1;
        WasPreOrdered = item.Status.Preordered ;
        IsOnWishList = item.Status.Wishlist = 1;

        MinimumPlayers = details.MinimumPlayers;
        MaximumPlayers = details.MaximumPlayers;
    }

    let getCollectionAsync (bggSettings : BoardGameGeekSettings) (client : HttpClient) =
        task {
            let! collectionXml =
                client |> getAsStringAsync(
                    "https://www.boardgamegeek.com"
                        .AppendPathSegments("xmlapi2", "collection")
                        .SetQueryParam("username", bggSettings.Username)
                        .SetQueryParam("showprivate", 1)
                        .SetQueryParam("stats", 1)
                        .SetQueryParam("own", 1)
                        .ToUri())

            let bggCollection = BggCollection.Parse(collectionXml)

            let bggDetails = 
                bggCollection.Items 
                |> Seq.map (fun i -> i.Objectid) 
                |> Seq.chunkBySize 50
                |> Seq.collect (fun ids -> (client |> getDetailsAsync ids).Result)
                |> Seq.toList

            return {
                OwnerUsername = bggSettings.Username;
                BoardGames =
                    bggCollection.Items
                    |> Seq.filter(fun i -> i.Subtype = "boardgame")
                    |> Seq.map(
                        fun i -> 
                            toBoardGame 
                                i 
                                (bggDetails |> Seq.find(fun d -> d.ObjectId = i.Objectid)))
                    |> Seq.toList}
        }