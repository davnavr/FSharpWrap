namespace rec FSharpWrap.Tool.Reflection

open System.Collections
open System.Collections.Generic

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

[<StructuralComparison; StructuralEquality>]
type TypeParam =
    { Constraints: GenericConstraints
      ParamName: FsName }

type TypeArgList = TypeArgList<TypeArg>

[<StructuralComparison; StructuralEquality>]
type TypeName =
    { Name: FsName
      Namespace: Namespace
      Parent: TypeName option
      TypeArgs: TypeArgList }

[<StructuralComparison; StructuralEquality>]
type TypeRef =
    | ArrayType of
        {| ElementType: TypeArg
           Rank: uint |}
    | ByRefType of TypeArg
    | PointerType of TypeArg
    | TypeName of TypeName

[<StructuralComparison; StructuralEquality>]
type TypeArg =
    | TypeArg of TypeRef
    | TypeParam of TypeParam

type ParamOptional =
    | FsOptionalParam
    | OptionalParam
    | RequiredParam

type Param =
    { ArgType: TypeArg
      IsOptional: ParamOptional
      ParamName: FsName }

type ReadOnly = ReadOnly | Mutable

type Field =
    { FieldName: string
      FieldType: TypeArg
      IsReadOnly: ReadOnly }

type Method =
    { MethodName: string
      Params: Param list
      RetType: TypeArg
      TypeArgs: TypeArgList }

// TODO: How to handle properties with parameters, maybe handle them as methods instead?
type Property =
    { PropName: string
      PropType: TypeArg
      Setter: bool }

type Member =
    | Constructor of Param list
    | InstanceField of Field
    | InstanceMethod of Method
    | InstanceProperty of Property
    | StaticField of Field
    | StaticMethod of Method
    | StaticProperty of Property
    | UnknownMember of name: string

type TypeDef =
    { Members: Member list
      TypeName: TypeName }

type AssemblyInfo =
    { FullName: string
      Types: TypeDef list }
