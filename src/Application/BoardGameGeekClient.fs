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
    type BggPlays = XmlProvider<"Samples/plays-sample.xml">

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

    let xn s = XName.Get(s)

    let toBoardGame (item : BggCollection.Item) (detail : BggDetails.Item) (plays : BggPlays.Play list) = {
        ObjectId = item.Objectid;
        Name = item.Name.Value;
        YearPublished = item.Yearpublished;
        Type = detail.Type;

        AcquisitionDate = item.Privateinfo |> Option.bind (fun x -> Some(x.Acquisitiondate));

        MyRating = 
            item.Stats.Rating.Value.String 
            |> Option.bind (fun x -> if x <> "N/A" then Some (Decimal.Parse(x)) else None);

        AverageRating = item.Stats.Rating.Average.Value;
        GeekRating = item.Stats.Rating.Bayesaverage.Value;
        NumberOfRatings = item.Stats.Rating.Usersrated.Value;

        OverallRank = 
            item.Stats.Rating.Ranks 
            |> Seq.find (fun r -> r.Name = "boardgame")
            |> (fun x -> if x.Value.Value <> "Not Ranked" then Int32.Parse(x.Value.Value) else 0);

        CategoryRank = 
            match item.Stats.Rating.Ranks |> Seq.tryFind (fun r -> r.Type = "family") with
            | Some (r) -> if r.Value.Value <> "Not Ranked" then Int32.Parse(r.Value.Value) else 0
            | None -> 0;

        IsOwned = item.Status.Own = 1;
        WasOwned = item.Status.Prevowned = 1;
        DoesWant = item.Status.Want = 1;
        WasPreOrdered = item.Status.Preordered ;
        IsOnWishList = item.Status.Wishlist = 1;

        PlayCount = item.Numplays;

        MinimumPlayers = detail.Minplayers.Value;
        MaximumPlayers = detail.Maxplayers.Value;

        PlayingTime = detail.Playingtime.Value;
        MinimumPlayingTime = detail.Minplaytime.Value;
        MaximumPlayingTime = detail.Maxplaytime.Value;

        MinimumAge = detail.Minage.Value;
        AverageWeight = detail.Statistics.Ratings.Averageweight.Value;
        NumberOfWeights = detail.Statistics.Ratings.Numweights.Value;

        Mechanics = 
            detail.Links 
            |> Seq.filter (fun l -> l.Type = "boardgamemechanic") 
            |> Seq.map (fun l -> l.Value) 
            |> Seq.toList

        Plays =
            plays
            |> Seq.map (
                fun play -> {
                    Id = play.Id;
                    ObjectId = play.Item.Objectid;
                    Date = play.Date;
                    Quantity = play.Quantity;
                    Location = play.Location;
                    Players =
                        play.Players 
                        |> Option.bind (
                            fun players -> 
                                Some(
                                    players.Players 
                                    |> Seq.map (
                                        fun x -> {
                                            Username = x.Username.Value;
                                            UserId = x.Userid;
                                            Name = x.Name;
                                            Score = x.Score;
                                            Rating = x.Rating;
                                            DidWin = x.Win;
                                        }) 
                                    |> Seq.toList))
                })
            |> Seq.toList
    }

    let getCollectionDataAsync (bggSettings : BoardGameGeekSettings) (client : HttpClient) =
        task {
            let! collectionXml =
                client |> getAsStringAsync (
                    "https://www.boardgamegeek.com"
                        .AppendPathSegments("xmlapi2", "collection")
                        .SetQueryParam("username", bggSettings.Username)
                        .SetQueryParam("showprivate", 1)
                        .SetQueryParam("stats", 1)
                        .SetQueryParam("own", 1)
                        .ToUri())

            return BggCollection.Parse (collectionXml)
        }

    let getDetailDataAsync (objectIds : int[]) (client : HttpClient) =
        task {
            let! detailsXml = 
                client |> getAsStringAsync (
                    "https://www.boardgamegeek.com"
                        .AppendPathSegment("xmlapi2")
                        .AppendPathSegment("thing")
                        .SetQueryParam("id", String.Join(",", objectIds))
                        .SetQueryParam("stats", 1)
                        .ToUri())

            return (BggDetails.Parse (detailsXml)).Items |> Seq.toList
        }

    let getPlayDataASync (bggSettings : BoardGameGeekSettings) (client : HttpClient) =
        task {
            let! playsXml =
                client |> getAsStringAsync(
                    "https://www.boardgamegeek.com"
                        .AppendPathSegment("xmlapi2")
                        .AppendPathSegment("plays")
                        .SetQueryParam("username", bggSettings.Username)
                        .ToUri())

            return (BggPlays.Parse (playsXml)).Plays |> Seq.toList
        }

    let getCollectionAsync (bggSettings : BoardGameGeekSettings) (client : HttpClient) =
        task {
            // get the collection
            let! bggCollection = client |> getCollectionDataAsync bggSettings

            // get additional details 50 at a time
            let bggDetails = 
                bggCollection.Items 
                |> Seq.map (fun i -> i.Objectid) 
                |> Seq.chunkBySize 50
                |> Seq.collect (fun ids -> (client |> getDetailDataAsync ids).Result)
                |> Seq.toList

            // get all of the plays
            let! bggPlays = client |> getPlayDataASync bggSettings

            // convert the data to the Collection object
            return {
                OwnerUsername = bggSettings.Username;
                BoardGames =
                    bggCollection.Items
                    |> Seq.filter (fun i -> i.Subtype = "boardgame")
                    |> Seq.map(
                        fun i -> 
                            toBoardGame 
                                i 
                                (bggDetails |> Seq.find (fun d -> d.Id = i.Objectid))
                                (bggPlays |> Seq.filter (fun p -> p.Item.Objectid = i.Objectid) |> Seq.toList))
                    |> Seq.toList}
        }