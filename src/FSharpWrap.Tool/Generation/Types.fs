﻿namespace FSharpWrap.Tool.Generation

open System

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

// "TODO: Should AttributeInfo be used instead?"
type GenAttribute =
    { Arguments: string list
      AttributeType: TypeName }

[<CustomComparison; CustomEquality>] // TODO: Allow bindings to have attribute information.
type GenBinding =
    | GenActivePattern of
        {| Attributes: GenAttribute list
           Body: string
           Parameters: (FsName * TypeArg) list
           PatternName: FsName |}
    | GenFunction of
        {| Attributes: GenAttribute list
           Body: string
           Name: FsName
           Parameters: (FsName * TypeArg) list |}

    member this.Attributes =
        match this with
        | GenActivePattern pattern -> pattern.Attributes
        | GenFunction func -> func.Attributes

    override this.Equals obj =
        match this, obj :?> GenBinding with
        | GenFunction this, GenFunction other ->
            this.Name = other.Name
        | GenActivePattern this, GenActivePattern other ->
            this.PatternName = other.PatternName
        | _ -> false
    override this.GetHashCode() =
        match this with
        | GenActivePattern pattern -> Choice1Of2 pattern.PatternName
        | GenFunction func -> Choice2Of2 func.Name
        |> hash

    interface IComparable with
        member this.CompareTo obj =
            match this, obj :?> GenBinding with
            | GenFunction this, GenFunction other ->
                compare this.Name other.Name
            | GenActivePattern this, GenActivePattern other ->
                compare this.PatternName other.PatternName
            | GenActivePattern _, _ -> -1
            | _, GenActivePattern _ -> 1

[<CustomComparison; CustomEquality>]
type GenModule =
    { Attributes: GenAttribute list
      Bindings: Set<GenBinding>
      ModuleName: FsName }

    override this.Equals obj = this.ModuleName = (obj :?> GenModule).ModuleName
    override this.GetHashCode() = this.ModuleName.GetHashCode()

    interface IComparable with
        member this.CompareTo obj = compare this.ModuleName (obj :?> GenModule).ModuleName

type GenNamespace = Map<FsName, GenModule>

type GenFile =
    { Header: seq<string>
      Namespaces: Map<Namespace, GenNamespace> }
