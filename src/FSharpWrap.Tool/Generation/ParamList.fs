namespace FSharpWrap.Tool.Generation

open System.Collections
open System.Collections.Generic
open System.ComponentModel

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

type GenParam = FsName * Param

[<RequireQualifiedAccess>]
module ParamList =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    type ParamList =
        private
        | ParamList of GenParam list * Set<FsName>

        member private this.Items =
            let (ParamList(plist, _)) = this in plist
        interface IEnumerable<GenParam> with
            member this.GetEnumerator() = (this.Items :> IEnumerable<_>).GetEnumerator()
        interface IEnumerable with
            member this.GetEnumerator() = (this.Items :> IEnumerable).GetEnumerator()

    let private safe set { Param.ParamName = FsName name } =
        let lname = String.toCamelCase name |> FsName
        let sname =
            if Set.contains lname set
            then sprintf "%O'" lname |> FsName
            else lname
        sname, Set.add sname set

    let empty = ParamList([], Set.empty)

    let singleton ({ Param.ParamName = name } as param) =
        ParamList([ name, param ], Set.singleton name)

    let append param (ParamList(list, set)) =
        let name, set' = safe set param
        let param' = name, param
        ParamList(list @ [ param' ], set'), param'

    let toList (ParamList(list, _)) = list

    let ofList =
        let rec inner set list =
            function
            | [] -> ParamList(List.rev list, set)
            | (h: Param) :: tail ->
                let name, set' = safe set h
                let list' = (name, h) :: list
                inner set' list' tail
        inner Set.empty []

type ParamList = ParamList.ParamList

[<AutoOpen>]
module ParamListPatterns =
    let (|EmptyParams|ParamList|) plist =
        match ParamList.toList plist with
        | [] -> Choice1Of2()
        | plist' -> Choice2Of2 plist'
