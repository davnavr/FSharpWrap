namespace FSharpWrap.Tool.Reflection

open System

type TypeParam =
    { Name: string }

[<CustomComparison; CustomEquality>]
type TypeRef =
    { Name: SimpleName
      Namespace: Namespace
      Parent: TypeRef option
      TypeArgs: TypeArg list }

    member this.FullName =
        let targs { TypeArgs = args } =
            match args with
            | [] -> ""
            | _ -> List.length args |> sprintf "`%i"
        let ns =
            match this.Namespace with
            | Namespace [] -> ""
            | ns -> sprintf "%O." ns
        let parent =
            Option.map
                (fun parent ->
                    sprintf
                        "%O%s+"
                        parent.Name
                        (targs parent))
                this.Parent
            |> Option.defaultValue ""
        sprintf
            "%s%s%O%s"
            ns
            parent
            this.Name
            (targs this)

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

[<CustomComparison; CustomEquality>]
type Param =
    { ArgType: TypeArg
      ParamName: SimpleName } // TODO: Use separate type for ParamName.

    override this.GetHashCode() = this.ParamName.GetHashCode()

    override this.Equals obj =
        this.ParamName = (obj :?> Param).ParamName

    interface IComparable with
        member this.CompareTo obj =
            compare this.ParamName (obj :?> Param).ParamName

type Field =
    { FieldName: string
      FieldType: TypeRef }

type Method =
    { MethodName: string
      Params: Param list
      RetType: TypeRef
      TypeParams: TypeParam list }

// TODO: How to handle properties with parameters?
type Property =
    { PropName: string
      PropType: TypeRef
      Setter: bool }

type Member =
    | Constructor of param: TypeRef list
    | InstanceField of Field
    | InstanceMethod of Method
    | InstanceProperty of Property
    | StaticField of Field
    | StaticMethod of Method
    | StaticProperty of Property
    | UnknownMember of name: string

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
