namespace Misc.BgStats.Domain

open System

module Models =
    type BoardGameGeekSettings() =
        member val Url : Uri = Unchecked.defaultof<Uri> with get, set
        member val Username = String.Empty with get, set
        member val Password = String.Empty with get, set

    type Settings() =
        member val BoardGameGeek = BoardGameGeekSettings() with get, set

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
    }

    type Collection = {
        OwnerUsername : string
        BoardGames : BoardGame list
    }