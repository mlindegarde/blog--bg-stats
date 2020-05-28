namespace Misc.BgStats.AverageScore.Domain

open System

module Models =
    type Player = {
        Username : string
        UserId : int
        Name : string
        Score : int
        Rating : int
        DidWin : bool
    }

    type Play = {
        Id : int
        ObjectId : int
        Date : DateTime
        Quantity : int
        Location : string
        Players : Player[]
    }