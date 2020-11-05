namespace rec FSharpWrap.Tool.Reflection

open System.Collections
open System.Collections.Generic

open FSharpWrap.Tool

[<RequireQualifiedAccess>]
type AttributeArg =
    | Array of (TypeRef * AttributeArg) list
    | Bool of bool
    | Char of char
    | Double of System.Double
    | Int8 of int8
    | Int16 of int16
    | Int32 of int32
    | Int64 of int64
    | Null
    | Single of System.Single
    | String of string
    | Type of TypeRef
    | UInt8 of uint8
    | UInt16 of uint16
    | UInt32 of uint32
    | UInt64 of uint64

type AttributeInfo =
    { AttributeType: TypeName
      ConstructorArgs: (TypeRef * AttributeArg) list
      NamedArgs: Map<FsName, TypeRef * AttributeArg> }

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

type MemberType =
    | Constructor of Param list
    | InstanceField of Field
    | InstanceMethod of Method
    | InstanceProperty of Property
    | StaticField of Field
    | StaticMethod of Method
    | StaticProperty of Property
    | UnknownMember of name: string

type Member =
    { Attributes: AttributeInfo list
      Type: MemberType }

[<CustomComparison; CustomEquality>]
type TypeDef =
    { Attributes: AttributeInfo list
      Members: Member list
      TypeName: TypeName }

    override this.Equals obj = this.TypeName = (obj :?> TypeDef).TypeName
    override this.GetHashCode() = this.TypeName.GetHashCode()

    interface System.IComparable with
        member this.CompareTo obj =
            compare this.TypeName (obj :?> TypeDef).TypeName

type AssemblyInfo =
    { FullName: string
      Types: TypeDef list }
