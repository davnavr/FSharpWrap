namespace FSharpWrap.Tool.Generation

open System

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

type GenAttribute =
    { Arguments: string list
      AttributeType: TypeName }

[<CustomComparison; CustomEquality>]
type GenBinding =
    | GenFunction of
        {| Body: string
           Name: FsName
           Parameters: (FsName * TypeArg) list |}

    member private this.Name = let (GenFunction func) = this in func.Name

    override this.Equals obj = this.Name = (obj :?> GenBinding).Name
    override this.GetHashCode() = this.Name.GetHashCode()

    interface IComparable with
        member this.CompareTo obj = compare this.Name (obj :?> GenBinding).Name

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
