namespace FSharpWrap.Tool.Generation

open System

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

type GenAttribute =
    { Arguments: string list
      AttributeType: TypeName }

[<CustomComparison; CustomEquality>]
type GenBinding =
    | GenActivePattern of
        {| Body: string
           Parameters: (FsName * TypeArg) list
           PatternName: FsName |}
    | GenFunction of
        {| Body: string
           Name: FsName
           Parameters: (FsName * TypeArg) list |}

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
