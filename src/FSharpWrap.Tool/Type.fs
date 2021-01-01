namespace rec FSharpWrap.Tool

open System

[<StructuralComparison; StructuralEquality>]
type TypeParam =
    { //Constraints: GenericConstraints // TODO: Add constraints for type parameters.
      ParamName: FsName }

[<StructuralComparison; StructuralEquality>]
type TypeName = // TODO: Should this be a struct?
    { Name: FsName
      Namespace: Namespace
      Parent: TypeName option
      TypeArgs: TypeArg[] }

[<StructuralComparison; StructuralEquality>]
type TypeArg =
    | ArrayType of
           {| ElementType: TypeArg
              Rank: uint |}
    | ByRefType of TypeArg
    | FsFuncType of TypeArg * TypeArg
    | InferredType
    | PointerType of TypeArg
    | TypeName of TypeName
    | TypeParam of TypeParam

type TypeIdentifier =
    | SingleType of Type
    | MultipleTypes of Map<uint32, Type>

[<RequireQualifiedAccess>]
module TypeName =
    /// Compares types by their name, namespace, and parent, ignoring any generic arguments.
    let comparer =
        { new System.Collections.Generic.IEqualityComparer<TypeName> with
            member _.Equals(x, y) =
                x.Name = y.Name && x.Namespace = y.Namespace && x.Parent = y.Parent
            member _.GetHashCode obj =
                hash (obj.Name, obj.Namespace, obj.Parent) }
