namespace FSharpWrap.Tool.Generation

open System
open System.ComponentModel

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

[<RequireQualifiedAccess>]
module ParamList =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    type ParamList =
        private
        | ParamList of Param list * Set<FsName>

    let private safeName set ({ Param.ParamName = FsName name } as param) =
        // TODO: Factor out common code for turning something into camelCase.
        let lname =
            String.mapi
                (function
                | 0 -> Char.ToLower
                | _ -> id)
                name
            |> FsName
        let sname =
            if Set.contains lname set
            then sprintf "%O'" lname |> FsName
            else lname
        { param with ParamName = sname }, Set.add sname set

    let empty = ParamList([], Set.empty)

    let singleton param =
        ParamList([ param ], Set.singleton param.ParamName)

    let append param (ParamList (list, set)) =
        let param', set' = safeName set param
        ParamList(list @ [ param' ], set')

    let toList (ParamList (list, _)) = list

    let ofList =
        let rec inner set list =
            function
            | [] -> ParamList (List.rev list, set)
            | (h: Param) :: tail ->
                let param, set' = safeName set h
                let list' = param :: list
                inner set' list' tail
        inner Set.empty []

    let print =
        function
        | ParamList([], _) -> "()"
        | ParamList(list, _) ->
            List.map
                Print.param
                list
            |> String.concat " "

type ParamList = ParamList.ParamList
