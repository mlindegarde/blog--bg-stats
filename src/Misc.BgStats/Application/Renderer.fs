namespace Misc.BgStats.Application

open System

open Misc.BgStats.Domain.Models

module Renderer =
    let private getScore (scoreType : ScoreType) (scores : Score list) =
        (scores |> List.find (fun s -> s.ScoreType = scoreType)).Value

    let private displayTopLine (longestName : int) =
        printf "\u2554"
        [0..3] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..longestName+1] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printfn "\u2557"

        longestName

    let private displayHeaders (longestName : int) =
        printfn
            "\u2551  # \u2502 %-*s \u2502      T \u2502      R \u2502      P \u2502      O \u2551"
            longestName
            "TITLE"

        longestName

    let private displayHeaderLine (longestName : int) =
        printf "\u2560"
        [0..3] |> List.iter (fun _ -> printf "\u2550")
        printf "\u256A"
        [0..longestName+1] |> List.iter (fun _ -> printf "\u2550")
        printf "\u256A"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u256A"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u256A"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u256A"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printfn "\u2563"

    let private displayBottomLine (longestName : int) =
        printf "\u255A"
        [0..3] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..longestName+1] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printfn "\u255D"

    let private displayResults (title : string, limit : int) (rankings : Ranking list) =
        printf "%s%s%s" Environment.NewLine title Environment.NewLine

        let topN = rankings |> List.take (limit)

        let longestName = 
            topN 
            |> List.map (fun r -> r.BoardGame.Name.Length)
            |> List.max

        longestName
        |> displayTopLine
        |> displayHeaders
        |> displayHeaderLine

        topN
        |> List.iter (fun r ->
            printfn 
                "\u2551%3d \u2502 %-*s \u2502 %6.2f \u2502 %6.2f \u2502 %6.2f \u2502 %6.2f \u2551"
                r.Position
                longestName
                r.BoardGame.Name
                (r.Evaluation.Scores |> List.sumBy (fun s -> s.Value))
                (r.Evaluation.Scores |> getScore ScoreType.Rating)
                (r.Evaluation.Scores |> getScore ScoreType.Plays)
                (r.Evaluation.Scores |> getScore ScoreType.Ownership))

        longestName |> displayBottomLine

    let displayTopNGames (limit : int) = displayResults (sprintf "Top %d Games" limit, limit)
    let displayTop25Games = 25 |> displayTopNGames

    let displayNGamesToPlay (limit : int) = displayResults(sprintf "Top %d Games to Play" limit, limit)
    let display15GamesToPlay = 15 |> displayNGamesToPlay

    let displayNGamesToSellOrTrade (limit : int) = displayResults (sprintf "Top %d Games to Sell / Trade" limit, limit)
    let display15GamesToSellOrTrade = 15 |> displayNGamesToSellOrTrade