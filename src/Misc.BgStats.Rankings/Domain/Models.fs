namespace Misc.BgStats.Rankings.Domain

open System

module Models =
    type BoardGameGeekSettings() =
        member val Url : Uri = Unchecked.defaultof<Uri> with get, set
        member val Username = String.Empty with get, set
        member val Password = String.Empty with get, set

    type Settings() =
        member val BoardGameGeek = BoardGameGeekSettings() with get, set

    type Player = {
        Username : string
        UserId : int
        Name : string
        Score : int option
        Rating : int
        DidWin : bool
    }

    type Play = {
        Id : int
        ObjectId : int
        Date : DateTime
        Quantity : int
        Location : string
        Players : Player list option
    }

    type BoardGame = {
        ObjectId : int
        Name : string
        YearPublished : int
        Type : string

        AcquisitionDate : DateTime option

        MyRating : decimal option
        AverageRating : decimal
        GeekRating : decimal
        NumberOfRatings : int

        OverallRank : int
        CategoryRank : int

        IsOwned : bool
        WasOwned : bool
        DoesWant : bool
        WasPreOrdered : bool
        IsOnWishList : bool

        PlayCount : int

        MinimumPlayers : int
        MaximumPlayers : int

        PlayingTime : int
        MinimumPlayingTime : int
        MaximumPlayingTime : int

        MinimumAge : int
        AverageWeight : decimal
        NumberOfWeights : int

        Mechanics : string list
        Plays : Play list
    }

    type Collection = {
        OwnerUsername : string
        BoardGames : BoardGame list
    }

    type ScoreType = Undefined = 0 | Plays = 1 | Rating = 2 | Ownership = 3

    type Score = {
        ScoreType : ScoreType
        Value : double
    }

    type Evaluation = {
        BoardGame : BoardGame
        DaysSinceLastPlayed : double
        AverageDaysBetweenPlays : double
        Scores : Score list
    }

    type Ranking = {
        Position : int
        BoardGame : BoardGame
        Evaluation : Evaluation
    }