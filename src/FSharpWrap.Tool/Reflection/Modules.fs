namespace rec FSharpWrap.Tool.Reflection

open System.Reflection

open FSharpWrap.Tool

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module private GenericConstraints =
    let empty() = { GenericConstraints.Constraints = Set.empty }
    let update (constraints: GenericConstraints) items = constraints.Constraints <- items

[<AutoOpen>]
module Patterns =
    let (|HasGenericConstraints|_|) (tparam: TypeParam) =
        match tparam.Constraints with
        | { Constraints = Empty } -> None
        | _ -> Some tparam.Constraints

    let (|IsNamedType|_|) ns name =
        function
        | TypeName t when t.Name = FsName name && t.Namespace = Namespace.ofStr ns ->
            Some t
        | _ -> None

[<RequireQualifiedAccess>]
module Type =
    let private typeName (GenericArgs gargs as t) =
        context {
            let parent = Option.ofObj t.DeclaringType
            let! parent' =
                parent
                |> Option.map name
                |> Option.defaultValue (Context.retn None)
            let! gargs' =
                let notInherited =
                    match parent with
                    | None -> fun _ -> true
                    | Some (GenericArgs inherited) ->
                        fun garg -> Array.contains garg inherited |> not
                fun ctx ->
                    Seq.where
                        notInherited
                        gargs
                    |> Seq.mapFold
                        (fun ctx' garg -> arg garg ctx')
                        ctx
            return
                { Name = FsName.ofType t
                  Namespace = Namespace.ofStr t.Namespace
                  Parent = parent'
                  TypeArgs = TypeArgList.ofSeq gargs' }
        }

    let private typeRef t =
        match t with
        | IsArray elem ->
            context {
                let! etype = arg elem
                return
                    {| ElementType = etype
                       Rank = t.GetArrayRank() |> uint |}
                    |> ArrayType
            }
        | IsByRef tref ->
            context {
                let! tref' = arg tref in return ByRefType tref'
            }
        | IsPointer pnt ->
            context {
                let! pnt' = arg pnt in return PointerType pnt'
            }
        | _ -> typeName t |> Context.map TypeName

    let ref t =
        Context.map
            (function
            | TypeArg tref -> Some tref
            | _ -> None)
            (arg t)

    let name t =
        Context.map
            (Option.bind
                (function
                | TypeName tname -> Some tname
                | _ -> None))
            (ref t)

    let arg t =
        context {
            match! Context.current with
            | HasType t existing -> return existing
            | _ ->
                match t with
                | GenericParam constraints as gen ->
                    let param =
                        { Constraints = GenericConstraints.empty()
                          ParamName = FsName gen.Name }
                    do! fun ctx -> { ctx with TypeParams = ctx.TypeParams.Add(t, param) }
                    let! constraints' =
                        fun (ctx: Context) ->
                            Seq.mapFold
                                (fun ctx' tc ->
                                    let c, ctx'' = arg tc ctx'
                                    TypeConstraint c, ctx'')
                                ctx
                                constraints
                    Set.ofSeq constraints' |> GenericConstraints.update param.Constraints
                    return TypeParam param
                | t ->
                    let! tref = typeRef t
                    do! fun ctx -> { ctx with TypeRefs = ctx.TypeRefs.Add(t, tref) }
                    return TypeArg tref
        }

    let def t =
        context {
            let! tname = name t |> Context.map Option.get
            let! members =
                fun ctx ->
                    t.GetMembers()
                    |> Seq.ofArray
                    |> Seq.where (fun m -> m.DeclaringType = t)
                    |> Seq.choose
                        (function
                        | IsCompilerGenerated
                        | IsSpecialName
                        | PropAccessor -> None
                        | mber -> Some mber)
                    |> Seq.mapFold
                        (fun ctx' mber -> Member.ofInfo mber ctx')
                        ctx
            return
                { Members = List.ofSeq members
                  TypeName = tname }
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module AssemblyInfo =
    let ofAssembly (assm: Assembly) =
        context {
            let! types =
                fun ctx ->
                    assm.ExportedTypes
                    |> Seq.choose
                        (function // TODO: Move type filtering logic outside of the reflection module.
                        | Derives "System" "Delegate" _
                        | AssignableTo "Microsoft.FSharp.Core" "FSharpFunc`2" _
                        | IsStatic _
                        | IsNested
                        | IsTuple _ -> None
                        | t -> Some t)
                    |> Seq.mapFold
                        (fun ctx' t -> Type.def t ctx')
                        ctx
            return
                { FullName = assm.FullName
                  Types = types |> List.ofSeq }
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Member =
    let ofInfo (info: MemberInfo) =
        let membertype cond inst stat =
            if cond then stat else inst
        let mthdparams (m: MethodBase) =
            fun ctx ->
                m.GetParameters()
                |> List.ofArray
                |> List.mapFold
                    (fun ctx' pinfo ->
                        ctx'
                        |> context {
                            let! argtype = Type.arg pinfo.ParameterType
                            return
                                { ArgType = argtype
                                  IsOptional =
                                    if pinfo.IsOptional
                                    then OptionalParam
                                    else
                                        pinfo.GetCustomAttributesData()
                                        |> Attribute.find
                                            "Microsoft.FSharp.Core"
                                            "OptionalArgumentAttribute"
                                            (fun _ -> Some FsOptionalParam)
                                        |> Option.defaultValue RequiredParam
                                  ParamName = FsName.ofParameter pinfo }
                        })
                    ctx
        match info with
        | :? ConstructorInfo as ctor ->
            mthdparams ctor |> Context.map Constructor
        | :? FieldInfo as field ->
            context {
                let! ftype = Type.arg field.FieldType
                return
                    { FieldName = field.Name
                      FieldType = ftype
                      IsReadOnly =
                        if field.Attributes.HasFlag FieldAttributes.InitOnly
                        then ReadOnly
                        else Mutable }
                    |> membertype
                        (field.Attributes.HasFlag FieldAttributes.Static)
                        InstanceField
                        StaticField
            }
        | :? PropertyInfo as prop when prop.GetIndexParameters() |> Array.isEmpty ->
            context {
                let! ptype = Type.arg prop.PropertyType
                return
                    { PropName = prop.Name
                      Setter = prop.CanRead
                      PropType = ptype }
                    |> membertype
                            ((prop.GetAccessors() |> Array.head).Attributes.HasFlag MethodAttributes.Static)
                            InstanceProperty
                            StaticProperty
            }
        | :? MethodInfo as mthd ->
            context {
                let! paramts = mthdparams mthd
                let! ret = Type.arg mthd.ReturnType
                let! targs = // TODO: Factor out common code for retrieving generic argument information.
                    fun ctx ->
                        mthd.GetGenericArguments()
                        |> Array.mapFold
                            (fun ctx' garg -> Type.arg garg ctx')
                            ctx
                return
                    { MethodName = mthd.Name
                      Params = paramts
                      RetType = ret
                      TypeArgs = TypeArgList.ofArray targs }
                    |> membertype
                        (mthd.Attributes.HasFlag MethodAttributes.Static)
                        InstanceMethod
                        StaticMethod
            }
        | _ -> UnknownMember info.Name |> Context.retn
