namespace Misc.BgStats.Domain

open System

module Models =
    type BoardGameGeekSettings() =
        member val Url : Uri = Unchecked.defaultof<Uri> with get, set
        member val Username = String.Empty with get, set
        member val Password = String.Empty with get, set

    type Settings() =
        member val BoardGameGeek = BoardGameGeekSettings() with get, set

    type Details = {
        ObjectId : int
        MinimumPlayers : int
        MaximumPlayers : int
    }

    type BoardGame = {
        ObjectId : int
        Name : string
        YearPublished : int
        Type : string

        AcquisitionDate : Option<DateTime>

        MyRating : Option<decimal>
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

        MinimumPlayers : int
        MaximumPlayers : int
    }

    type Collection = {
        OwnerUsername : string
        BoardGames : list<BoardGame>
    }