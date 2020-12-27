namespace FSharpWrap.Tool

open System

[<StructuralComparison; StructuralEquality>]
type TypeName = // TODO: Should this be a struct?
    { Name: FsName
      Namespace: Namespace
      Parent: TypeName option }

[<RequireQualifiedAccess>]
module TypeName =
    let rec ofType (t: Type) =
        let parent = Option.ofObj t.DeclaringType
        { Name = FsName.ofType t
          Namespace = Namespace.ofStr t.Namespace
          Parent = Option.map ofType parent }
