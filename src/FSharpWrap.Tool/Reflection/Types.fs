namespace FSharpWrap.Tool.Reflection

open System

[<StructuralComparison; StructuralEquality>]
type TypeName =
    { Name: FsName
      Namespace: Namespace
      Parent: TypeName option
      TypeArgs: TypeArgList }

and TypeArgList = TypeArgList.TypeArgList<TypeArg>

and [<StructuralComparison; StructuralEquality>] TypeRef =
    | TypeName of TypeName
    | ArrayType of
        {| ElementType: TypeArg
           Rank: uint |}

and [<StructuralComparison; StructuralEquality>]
    TypeArg =
    | TypeArg of TypeRef
    | TypeParam

type Param =
    { ArgType: TypeArg
      ParamName: FsName }

type ReadOnly = ReadOnly | Mutable

type Field =
    { FieldName: string
      FieldType: TypeRef
      IsReadOnly: ReadOnly }

type Method =
    { MethodName: string * uint
      Params: Param list
      RetType: TypeRef }

// TODO: How to handle properties with parameters, maybe handle them as methods instead?
type Property =
    { PropName: string
      PropType: TypeRef
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

[<CustomComparison; CustomEquality>]
type TypeDef =
    { Members: Member list
      TypeName: TypeName }

    override this.Equals obj = this.TypeName = (obj :?> TypeDef).TypeName

    override this.GetHashCode() = this.TypeName.GetHashCode()

    interface IComparable with
        member this.CompareTo obj =
            compare this.TypeName (obj :?> TypeDef).TypeName

[<CustomComparison; CustomEquality>]
type AssemblyInfo =
    { FullName: string
      Types: TypeDef list }

    override this.Equals obj =
        this.FullName.Equals (obj :?> AssemblyInfo).FullName

    override this.GetHashCode() = this.FullName.GetHashCode()

    interface IComparable with
        member this.CompareTo obj =
            compare this.FullName (obj :?> AssemblyInfo).FullName
