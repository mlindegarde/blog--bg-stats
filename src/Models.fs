namespace Misc.BgStats

open System

module Models =
    type BoardGameGeekSettings() =
        member val Username = String.Empty with get, set
        member val Password = String.Empty with get, set

    type Settings() =
        member val BoardGameGeek = BoardGameGeekSettings() with get, set