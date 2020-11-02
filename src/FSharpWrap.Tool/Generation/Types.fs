namespace FSharpWrap.Tool.Generation

open System

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

type GenAttribute =
    { Arguments: string list
      AttributeType: TypeName }

type GenBinding =
    | GenFunction of
        {| Arguments: (FsName * TypeArg) list
           Body: string
           Name: FsName |}

[<CustomComparison; CustomEquality>]
type GenModule =
    { Attributes: GenAttribute list
      Bindings: GenBinding list
      ModuleName: FsName }

    override this.Equals obj = this.ModuleName = (obj :?> GenModule).ModuleName
    override this.GetHashCode() = this.ModuleName.GetHashCode()

    interface IComparable with
        member this.CompareTo obj = compare this.ModuleName (obj :?> GenModule).ModuleName

[<CustomComparison; CustomEquality>]
type GenNamespace =
    { Modules: Set<GenModule>
      Namespace: Namespace }

    override this.Equals obj = this.Namespace = (obj :?> GenNamespace).Namespace
    override this.GetHashCode() = this.Namespace.GetHashCode()

    interface IComparable with
        member this.CompareTo obj = compare this.Namespace (obj :?> GenNamespace).Namespace

type GenFile =
    { Header: seq<string>
      Namespaces: Set<GenNamespace> }
