namespace rec FSharpWrap.Tool.Reflection

open System
open System.Collections.ObjectModel
open System.Reflection

open FSharpWrap.Tool

[<RequireQualifiedAccess>]
module private Attributes =
    let private argType t =
        context {
            let! t' = Type.ref t
            return
                Option.defaultWith
                    (fun() -> invalidOp "Type parameters as attribute arguments are not supported")
                    t'
        }

    let private argValue: obj -> ContextExpr<_> =
        function
        | :? Type as t ->
            argType t |> Context.map AttributeArg.Type
        | :? ReadOnlyCollection<CustomAttributeTypedArgument> as items ->
            fun ctx ->
                let items', ctx' =
                    Seq.mapFold
                        (fun ctx'' item -> argument item ctx'')
                        ctx
                        items
                List.ofSeq items' |> AttributeArg.Array, ctx'
        | value ->
            match value with
            | null -> AttributeArg.Null
            | :? bool as b -> AttributeArg.Bool b
            | :? char as c -> AttributeArg.Char c
            | :? Double as d -> AttributeArg.Double d
            | :? int8 as i -> AttributeArg.Int8 i
            | :? int16 as i -> AttributeArg.Int16 i
            | :? int32 as i -> AttributeArg.Int32 i
            | :? int64 as i -> AttributeArg.Int64 i
            | :? Single as f -> AttributeArg.Single f
            | :? string as str -> AttributeArg.String str
            | :? uint8 as ui -> AttributeArg.UInt8 ui
            | :? uint16 as ui -> AttributeArg.UInt16 ui
            | :? uint32 as ui -> AttributeArg.UInt32 ui
            | :? uint64 as ui -> AttributeArg.UInt64 ui
            | err ->
                let t = err.GetType()
                sprintf
                    "Unsupported attribute argument %O of type %O"
                    value
                    t
                |> invalidOp
            |> Context.retn

    let argument (arg: CustomAttributeTypedArgument) =
        context {
            let! t = argType arg.ArgumentType
            let! value = argValue arg.Value
            return t, value
        }

    let private create (data: CustomAttributeData) =
        context {
            let! attrType = Type.name data.AttributeType
            let! ctorArgs =
                fun ctx ->
                    Seq.mapFold
                        (fun ctx' arg -> argument arg ctx')
                        ctx
                        data.ConstructorArguments
            let! namedArgs =
                fun ctx ->
                    Seq.mapFold
                        (fun ctx' (narg: CustomAttributeNamedArgument) ->
                            let value, ctx'' = argument narg.TypedValue ctx'
                            (FsName narg.MemberName, value), ctx'')
                        ctx
                        data.NamedArguments
            return
                { AttributeType =
                    Option.defaultWith
                        (fun() -> invalidOp "Invalid attribute type")
                        attrType
                  ConstructorArgs = List.ofSeq ctorArgs
                  NamedArgs = Map.ofSeq namedArgs }
        }

    let ofMember (mber: MemberInfo) ctx =
        mber.CustomAttributes
        |> List.ofSeq
        |> List.mapFold
            (fun ctx' attr -> create attr ctx')
            ctx

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
                        | IsObsoleteError
                        | IsSpecialName
                        | PropAccessor -> None
                        | mber -> Some mber)
                    |> Seq.mapFold
                        (fun ctx' mber -> Member.ofInfo mber ctx')
                        ctx
            let! attrs = Attributes.ofMember t
            return
                { Attributes = attrs
                  Members = List.ofSeq members
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
                    // TODO: Move type filtering logic for AssemblyInfo.ofAssembly and Type.def outside of the reflection namespace.
                    |> Seq.choose
                        (function
                        | Derives "System" "Delegate" _
                        | AssignableTo "Microsoft.FSharp.Core" "FSharpFunc`2" _
                        | IsNested
                        // NOTE: This filter currently excludes types such as ImmutableArray from code generation.
                        | IsMutableStruct
                        | IsStatic _
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
    let private getType (info: MemberInfo) =
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
                        if field.IsInitOnly
                        then ReadOnly
                        else Mutable }
                    |> membertype
                        field.IsStatic
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

    let ofInfo (mber: MemberInfo) =
        context {
            let! mtype = getType mber
            let! attrs = Attributes.ofMember mber
            return
                { Attributes = attrs
                  Member.Type = mtype }
        }
