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

    let toBoardGame (i : BggCollection.Item) = {
        ObjectId = i.Objectid;
        Name = i.Name.Value;
        YearPublished = i.Yearpublished;
        Type = i.Subtype;

        AcquisitionDate = i.Privateinfo |> Option.bind(fun x -> Some(x.Acquisitiondate));

        MyRating = 
            i.Stats.Rating.Value.String 
            |> Option.bind(fun x -> if x <> "N/A" then Some(Decimal.Parse(x)) else None);

        AverageRating = i.Stats.Rating.Average.Value;
        GeekRating = i.Stats.Rating.Bayesaverage.Value;
        NumberOfRatings = i.Stats.Rating.Usersrated.Value;

        OverallRank = 
            i.Stats.Rating.Ranks 
            |> Seq.find(fun r -> r.Name = "boardgame")
            |> (fun x -> if x.Value.Value <> "Not Ranked" then Int32.Parse(x.Value.Value) else 0);

        CategoryRank = 
            match i.Stats.Rating.Ranks |> Seq.tryFind (fun r -> r.Type = "family") with
            | Some(r) -> if r.Value.Value <> "Not Ranked" then Int32.Parse(r.Value.Value) else 0
            | None -> 0;

        IsOwned = i.Status.Own = 1;
        WasOwned = i.Status.Prevowned = 1;
        DoesWant = i.Status.Want = 1;
        WasPreOrdered = i.Status.Preordered ;
        IsOnWishList = i.Status.Wishlist = 1;
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

            return {
                OwnerUsername = bggSettings.Username;
                BoardGames =
                    bggCollection.Items
                    |> Seq.filter(fun i -> i.Subtype = "boardgame")
                    |> Seq.map(toBoardGame)
                    |> Seq.toList}
        }