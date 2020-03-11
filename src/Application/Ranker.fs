namespace Misc.BgStats.Application

open Misc.BgStats.Domain.Models

module Ranker =
    let RankByScore (evaluations : Evaluation list) =
        evaluations
        |> List.sortByDescending (fun e -> e.Scores |> List.sumBy (fun s -> s.Value))
        |> List.mapi (fun i e -> {
            Position = i + 1;
            BoardGame = e.BoardGame;
            Evaluation = e
        })