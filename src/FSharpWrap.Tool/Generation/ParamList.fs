namespace FSharpWrap.Tool.Generation

open System.ComponentModel

open FSharpWrap.Tool.Reflection

[<RequireQualifiedAccess>]
module ParamList =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    type ParamList =
        private
        | ParamList of Param list * Set<string>

    let private safeName set ({ ParamName = name } as param) =
        let name' =
            if Set.contains name set
            then sprintf "%s'" name
            else name
        { param with ParamName = name' }, Set.add name' set

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

    let print (ParamList (list, _)) =
        list
        |> List.map
            (fun { ArgType = argt; ParamName = name; } ->
                match argt with
                | TypeParam _ -> "_"
                | TypeArg t -> TypeRef.fsname t
                |> sprintf "(``%s``: %s)" name)
        |> String.concat " "

type ParamList = ParamList.ParamList
