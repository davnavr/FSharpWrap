namespace rec FSharpWrap.Tool

open System
open System.Collections
open System.Collections.Generic

[<StructuralComparison; StructuralEquality>]
type GenericConstraint =
    | TypeConstraint of TypeArg

[<CustomComparison; CustomEquality>]
type GenericConstraints =
    internal
    | GenericConstraints of Lazy<Set<GenericConstraint>>

    member private this.Constraints = let (GenericConstraints constraints) = this in constraints.Value

    member this.Count = this.Constraints.Count

    override this.GetHashCode() = this.Constraints.GetHashCode()

    override this.Equals obj = this.Constraints = (obj :?> GenericConstraints).Constraints

    interface IComparable with
        member this.CompareTo obj = (obj :?> GenericConstraints).Constraints |> compare this.Constraints
    interface IEnumerable<GenericConstraint> with
        member this.GetEnumerator() = (this.Constraints :> IEnumerable<_>).GetEnumerator()
    interface IEnumerable with
        member this.GetEnumerator() = (this.Constraints :> IEnumerable).GetEnumerator()

[<StructuralComparison; StructuralEquality>]
type TypeParam =
    { Constraints: GenericConstraints
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
