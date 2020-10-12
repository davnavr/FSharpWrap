namespace FSharpWrap.Tool.Reflection

open System

type TypeParam =
    { Name: string }

    override this.ToString() = this.Name

[<CustomComparison; CustomEquality>]
type TypeRef =
    { Name: string
      Namespace: Namespace
      Parent: TypeRef option
      TypeArgs: TypeArg list }

    member this.FullName =
        let ns =
            match this.Namespace with
            | Namespace [] -> ""
            | ns -> sprintf "%O." ns
        let parent = // TODO: Include type arguments of parent?
            this.Parent
            |> Option.map (fun parent -> sprintf "%s+" parent.Name)
            |> Option.defaultValue ""
        let targs =
            match this.TypeArgs with
            | [] -> ""
            | _ ->
                sprintf "`%i" (List.length this.TypeArgs)
        sprintf "%s%s%s%s" ns parent this.Name targs

    member private this.Info =
        this.Name, this.Namespace, this.Parent, List.length this.TypeArgs

    override this.Equals obj =
        let other = obj :?> TypeRef in this.Info = other.Info

    override this.GetHashCode() = hash this.Info

    interface IComparable with
        member this.CompareTo obj = compare this.Info (obj :?> TypeRef).Info
and [<StructuralComparison; StructuralEquality>]
    TypeArg =
    | TypeArg of TypeRef
    | TypeParam of TypeParam

type Field =
    { Name: string
      FieldType: TypeRef }

type Method =
    { Name: string
      Parameters: TypeRef list
      RetType: TypeRef
      TypeParams: TypeParam list }

// TODO: How to handle properties with parameters?
type Property =
    { Name: string
      Setter: bool
      PropType: TypeRef }

type InstanceMember =
    | Constructor of param: TypeRef list
    | InstanceField of Field
    | InstanceMethod of Method
    | InstanceProperty of Property

    member this.Name =
        match this with
        | Constructor _ -> ".ctor"
        | InstanceField { Name = name }
        | InstanceMethod { Name = name }
        | InstanceProperty { Name = name } ->
            name

type StaticMember =
    | StaticField of Field
    | StaticMethod of Method
    | StaticProperty of Property

    member this.Name =
        match this with
        | StaticField { Name = name }
        | StaticMethod { Name = name }
        | StaticProperty { Name = name } ->
            name

type Member =
    | InstanceMember of InstanceMember
    | StaticMember of StaticMember
    | UnknownMember of name: string

    member this.Name =
        match this with
        | InstanceMember i -> i.Name
        | StaticMember s -> s.Name
        | UnknownMember name -> name

[<CustomComparison; CustomEquality>]
type TypeDef =
    { Info: TypeRef
      Members: Member list }

    member this.FullName = this.Info.FullName
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
