namespace Misc.BgStats.Application

open System

open Misc.BgStats.Domain.Models

module Evaluator =
    let private calcDaysSinceLastPlayed (plays : Play list) =
        if plays.Length > 0 then
            (DateTime.Now - plays.Head.Date).TotalDays
        else
            0.0

    let private calcAverageTimeBetweenGames (plays : Play list) =
        if plays.Length > 0 then
            let newest = plays.Head
            let oldest = plays.[plays.Length-1]

            ((newest.Date - oldest.Date).TotalDays)/(float plays.Length)
        else
            0.0

    let calcRatingScore (boardGame : BoardGame) =
        match boardGame.MyRating with
        | Some(r) -> r * 0.7M + boardGame.AverageRating * 0.3M
        | None -> boardGame.GeekRating

    let calcPlayScore (averageDaysBetweenPlays : double, daysSinceLastPlay : double) (boardGame : BoardGame) =
        let deltaModifier =
            if averageDaysBetweenPlays >= 365.0 then
                0.3
            else if averageDaysBetweenPlays >= 180.0 then
                0.5
            else if averageDaysBetweenPlays >= 90.0 then
                0.8
            else if averageDaysBetweenPlays >= 30.0 then
                1.0
            else if averageDaysBetweenPlays >= 7.0 then
                1.2
            else
                0.5 

        let ageModifier =
            if daysSinceLastPlay >= 365.0 then
                0.3
            else if daysSinceLastPlay >= 180.0 then
                0.5
            else if daysSinceLastPlay >= 90.0 then
                0.8
            else if daysSinceLastPlay >= 30.0 then
                1.0
            else if daysSinceLastPlay >= 7.0 then
                1.2
            else
                1.5

        let weightModifier = (double boardGame.AverageWeight) / 5.0
        (double boardGame.PlayCount) * (deltaModifier * ageModifier * weightModifier)

    let calcOwnershipScore (boardGame : BoardGame) =
        match boardGame.AcquisitionDate with
        | Some(ad) ->
            let daysInMonth = 30.0
            let age = (DateTime.Now - ad).TotalDays

            match boardGame.MyRating with
            | None when boardGame.PlayCount = 0 -> -(age/90.0)
            | _ when boardGame.PlayCount > 0 -> 
                let daysPerPlay = age / (double boardGame.PlayCount)
                3.0 * daysInMonth / daysPerPlay
            | _ -> 0.0

        | None -> 0.0

    let private evaluateBoardGame (boardGame : BoardGame) =
        let sortedPlays = boardGame.Plays |> List.sortByDescending (fun p -> p.Date)
        let daysSinceLastPlayed = sortedPlays |> calcDaysSinceLastPlayed
        let averageDaysBetweenPlays = sortedPlays |> calcAverageTimeBetweenGames

        {
            BoardGame = boardGame;
            DaysSinceLastPlayed = daysSinceLastPlayed;
            AverageDaysBetweenPlays = averageDaysBetweenPlays;
            Scores = [
                {ScoreType = ScoreType.Rating; Value = (double (boardGame |> calcRatingScore))};
                {ScoreType = ScoreType.Plays; Value = boardGame |> calcPlayScore (averageDaysBetweenPlays, daysSinceLastPlayed)};
                {ScoreType = ScoreType.Ownership; Value = boardGame |> calcOwnershipScore};
            ]
        }

    let evaluate (collection : Collection) =
        collection.BoardGames |> List.map (evaluateBoardGame)