namespace Misc.BgStats.AverageScore.Services

open System

module Renderer =
    let private displayTopLine2 (longestName : int) =
        printf "\u2554"
        [0..longestName+1] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..8] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2564"
        [0..10] |> List.iter (fun _ -> printf "\u2550")
        printfn "\u2557"

        longestName

    let private displayHeaders2 (longestName : int) =
        printfn
            "\u2551 %-*s \u2502 PLAYERS \u2502 SAMPLE \u2502 AVG SCORE \u2551"
            longestName
            "TITLE"

        longestName

    let private displayHeaderLine2 (longestName : int) =
        printf "\u2560"
        [0..longestName+1] |> List.iter (fun _ -> printf "\u2550")
        printf "\u256A"
        [0..8] |> List.iter (fun _ -> printf "\u2550")
        printf "\u256A"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u256A"
        [0..10] |> List.iter (fun _ -> printf "\u2550")
        printfn "\u2563"

    let private displayBottomLine2 (longestName : int) =
        printf "\u255A"
        [0..longestName+1] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..8] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..7] |> List.iter (fun _ -> printf "\u2550")
        printf "\u2567"
        [0..10] |> List.iter (fun _ -> printf "\u2550")
        printfn "\u255D"

    let displayAverageScore (results : (string * int * int * double) list) =
        printf "%sAverage Scores%s" Environment.NewLine Environment.NewLine

        match results.Length > 0 with
        | true ->
            let name, _, _, _ = results.[0]

            name.Length
            |> displayTopLine2
            |> displayHeaders2
            |> displayHeaderLine2

            results
            |> List.iter (fun (name, playerCount, sampleSize, avgScore) -> 
                printfn 
                    "\u2551 %-*s \u2502 %7d \u2502 %6d \u2502 %9.2f \u2551" 
                    name.Length
                    name
                    playerCount 
                    sampleSize
                    avgScore)

            name.Length |> displayBottomLine2
        | false ->
            printf "%sNo data found%s" Environment.NewLine Environment.NewLine