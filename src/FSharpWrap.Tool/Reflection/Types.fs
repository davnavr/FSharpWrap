namespace FSharpWrap.Tool.Reflection

open System

type TypeParam =
    { Name: string }

type TypeRef =
    { Name: string
      Namespace: string
      TypeArgs: unit list }

type Field =
    { Name: string 
      FieldType: TypeRef }

type Method =
    { Name: string
      Parameters: TypeRef list
      RetType: TypeRef
      TypeParams: TypeParam list }

type Property =
    { Name: string
      Setter: bool
      PropType: TypeRef }

type InstanceMember =
    | Constructor of param: TypeRef list
    | InstanceField of Field
    | InstanceMethod of Method
    | InstanceProperty of Property

type StaticMember =
    | StaticField of Field
    | StaticProperty of Property
    | StaticMethod of Method

type Member =
    | InstanceMember of InstanceMember
    | StaticMember of StaticMember
    | UnknownMember of Reflection.MemberInfo

[<CustomComparison; CustomEquality>]
type TypeDef =
    { Info: TypeRef
      Members: Member list }

    member this.Name = this.Info.Name
    member this.Namespace = this.Info.Namespace

    override this.Equals obj = this.Info = (obj :?> TypeDef).Info

    override this.GetHashCode() = this.Info.GetHashCode()

    interface IComparable with
        member this.CompareTo obj = compare this.Info (obj :?> TypeDef).Info

[<CustomComparison; CustomEquality>]
type AssemblyInfo =
    { FullName: string
      Types: TypeDef list }

    override this.Equals obj =
        this.FullName.Equals (obj :?> AssemblyInfo).FullName

    override this.GetHashCode() = this.FullName.GetHashCode()

    interface IComparable with
        member this.CompareTo obj = compare this.FullName (obj :?> AssemblyInfo).FullName
