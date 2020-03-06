namespace Misc.BgStats.Domain

open System

module Models =
    type BoardGameGeekSettings() =
        member val Username = String.Empty with get, set
        member val Password = String.Empty with get, set

    type Settings() =
        member val BoardGameGeek = BoardGameGeekSettings() with get, set

    type BoardGame = {
        ObjectId : int
        Name : string
        YearPublished : int
    }

    type Collection = {
        OwnerUsername : string
        BoardGames : list<BoardGame>
    }