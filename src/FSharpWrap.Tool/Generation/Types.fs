namespace FSharpWrap.Tool.Generation

open System

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

// TODO: Should AttributeInfo be used instead?
type GenAttribute =
    { Arguments: string list
      AttributeType: TypeName }

[<CustomComparison; CustomEquality>]
type ExprCombine =
    { Combine: string -> string -> string
      One: TypeArg
      Two: TypeArg }

    override this.Equals obj =
        let other = obj :?> ExprCombine
        this.One = other.One && this.Two = other.Two

    override this.GetHashCode() = hash(this.One, this.Two)

    interface IComparable with
        member this.CompareTo obj =
            let other = obj :?> ExprCombine
            compare (this.One, this.Two) (other.One, other.Two)

[<CustomComparison; CustomEquality>]
type ExprYield =
    { Item: TypeArg
      Yield: string -> string }

    override this.Equals obj = this.Item = (obj :?> ExprYield).Item
    override this.GetHashCode() = this.Item.GetHashCode()

    interface IComparable with
        member this.CompareTo obj = compare this.Item (obj :?> ExprYield).Item

[<StructuralComparison; StructuralEquality>]
type ExprOperation =
    | Combine of ExprCombine
    | Delay of TypeArg
    | Run of TypeArg * string
    | Yield of ExprYield
    | Zero of TypeArg

[<CustomComparison; CustomEquality>]
type GenBinding =
    | GenActivePattern of
        {| Attributes: GenAttribute list
           Body: string
           Parameters: ParamList
           PatternName: FsName |}
    /// Represents a computation expression type
    | GenBuilder of
        {| Attributes: GenAttribute list
           Name: FsName
           Operations: Set<ExprOperation> |}
    | GenFunction of
        {| Attributes: GenAttribute list
           Body: string
           Name: FsName
           Parameters: ParamList |}

    member this.Attributes =
        match this with
        | GenActivePattern pattern -> pattern.Attributes
        | GenBuilder ce -> ce.Attributes
        | GenFunction func -> func.Attributes

    override this.Equals obj = // TODO: How to ensure that there is no conflict between name of CE and a function?
        match this, obj :?> GenBinding with
        | GenFunction this, GenFunction other ->
            this.Name = other.Name
        | GenActivePattern this, GenActivePattern other ->
            this.PatternName = other.PatternName
        | GenBuilder this, GenBuilder other ->
            this.Name = other.Name
        | _ -> false
    override this.GetHashCode() =
        match this with
        | GenActivePattern pattern -> Choice1Of3 pattern.PatternName
        | GenBuilder ce -> Choice2Of3 ce.Name
        | GenFunction func -> Choice2Of3 func.Name
        |> hash

    interface IComparable with
        member this.CompareTo obj =
            match this, obj :?> GenBinding with
            | GenFunction this, GenFunction other ->
                compare this.Name other.Name
            | GenActivePattern this, GenActivePattern other ->
                compare this.PatternName other.PatternName
            | GenBuilder this, GenBuilder other ->
                compare this.Name other.Name
            | GenActivePattern _, _
            | _, GenBuilder _ -> -1
            | _, GenActivePattern _
            | GenBuilder _, _ -> 1

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
      IgnoredWarnings: uint list
      Namespaces: Map<Namespace, GenNamespace> }

[<NoComparison; NoEquality>]
type Printer =
    { Close: unit -> unit
      Line: unit -> unit
      Write: string -> unit }

    interface IDisposable with member this.Dispose() = this.Close()
