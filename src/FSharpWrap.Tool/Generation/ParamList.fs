namespace FSharpWrap.Tool.Generation

open System.ComponentModel

open FSharpWrap.Tool.Reflection

[<RequireQualifiedAccess>]
module ParamList =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    type ParamList =
        private
        | ParamList of Param list * Set<FsName>

    let private safeName set ({ Param.ParamName = name } as param) =
        let name' =
            if Set.contains name set
            then sprintf "%O'"
            else string
            <| name
        { param with ParamName = FsName name' }, Set.add name set

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
