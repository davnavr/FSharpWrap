namespace rec FSharpWrap.Tool

open System.Collections
open System.Collections.Generic

type TypeArgList = TypeArgList<TypeArg>

[<StructuralComparison; StructuralEquality>]
type TypeName =
    { Name: FsName
      Namespace: Namespace
      Parent: TypeName option
      TypeArgs: TypeArgList }

[<StructuralComparison; StructuralEquality>]
type GenericConstraint =
    | TypeConstraint of TypeArg

[<RequireQualifiedAccess>]
type GenericConstraints =
    private { mutable Constraints: Set<GenericConstraint> }

    interface IEnumerable<GenericConstraint> with
        member this.GetEnumerator() = (this.Constraints :> IEnumerable<_>).GetEnumerator()
    interface IEnumerable with
        member this.GetEnumerator() = (this.Constraints :> IEnumerable).GetEnumerator()

    static member Empty() = { Constraints = Set.empty }

[<StructuralComparison; StructuralEquality>]
type TypeParam =
    { Constraints: GenericConstraints
      ParamName: FsName }

[<StructuralComparison; StructuralEquality>]
type TypeArg =
    | ArrayType of TypeArg * rank: uint
    | ByRefType of TypeArg
    | FsFuncType of TypeArg * TypeArg
    | InferredType
    | PointerType of TypeArg
    | TypeName of TypeName
    | TypeParam of TypeParam
