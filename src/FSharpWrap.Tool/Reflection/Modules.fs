namespace rec FSharpWrap.Tool.Reflection

open System.Reflection

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
                let inherited =
                    parent
                    |> Option.map (|GenericArgs|)
                    |> Option.defaultValue Array.empty
                fun ctx ->
                    gargs
                    |> Seq.except inherited
                    |> Seq.mapFold
                        (fun ctx' garg -> arg garg ctx')
                        ctx
            return
                { Name = FsName.ofType t
                  Namespace = Namespace.ofStr t.Namespace
                  Parent = parent'
                  TypeArgs =
                    gargs'
                    |> Seq.toList
                    |> TypeArgList.ofList }
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
            let! types = Context.types
            match types.TryGetValue t with
            | (true, arg) ->
                return arg
            | (false, _) ->
                match t with
                | GenericParam as gen -> 
                    return TypeParam { TypeParam.Name = FsName gen.Name }
                | t ->
                    let! tref = typeRef t |> Context.map TypeArg
                    return! fun ctx -> tref, { ctx with Types = ctx.Types.Add(t, tref) }
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
                        | NeverDebuggerBrowsable
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
                        (function
                        | Derives "System" "Delegate" _
                        | AssignableTo "Microsoft.FSharp.Core" "FSharpFunc`2" _ 
                        | IsNested -> None
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
        let membert cond inst stat =
            if cond then stat else inst
        match info with
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
                    |> membert
                        (field.Attributes.HasFlag FieldAttributes.Static)
                        InstanceField
                        StaticField
            }
        | _ -> UnknownMember info.Name |> Context.retn
    //let ofInfo (info: MemberInfo) cache =
    //    let membert cond inst stat =
    //        if cond then stat else inst
    //    let mparams (m: #MethodBase) =
    //        m.GetParameters()
    //        |> Seq.map (fun pinfo ->
    //            { ArgType = TypeCache.typeArg pinfo.ParameterType cache |> fst // TODO: Make computation expression?
    //              IsOptional =
    //                if pinfo.IsOptional
    //                then OptionalParam
    //                else
    //                    pinfo.GetCustomAttributesData()
    //                    |> Attribute.find
    //                        "Microsoft.FSharp.Core"
    //                        "OptionalArgumentAttribute"
    //                        (fun _ -> Some FsOptionalParam)
    //                    |> Option.defaultValue RequiredParam
    //              ParamName = FsName.ofParameter pinfo })
    //        |> List.ofSeq
    //    match info with
    //    | :? ConstructorInfo as ctor ->
    //        mparams ctor |> Constructor
    //    | :? FieldInfo as field ->
    //        { FieldName = field.Name
    //          FieldType = TypeRef.ofType field.FieldType
    //          IsReadOnly =
    //            if field.Attributes.HasFlag FieldAttributes.InitOnly
    //            then ReadOnly
    //            else Mutable }
    //        |> membert
    //            (field.Attributes.HasFlag FieldAttributes.Static)
    //            InstanceField
    //            StaticField
    //    | :? PropertyInfo as prop when prop.GetIndexParameters() |> Array.isEmpty ->
    //        { PropName = prop.Name
    //          Setter = prop.CanRead
    //          PropType = TypeRef.ofType prop.PropertyType }
    //        |> membert
    //             ((prop.GetAccessors() |> Array.head).Attributes.HasFlag MethodAttributes.Static)
    //             InstanceProperty
    //             StaticProperty
    //    | :? MethodInfo as mthd ->
    //        { MethodName = mthd.Name, 0u // TODO: Type parameters.
    //          Params = mparams mthd
    //          RetType = TypeRef.ofType mthd.ReturnType }
    //        |> membert
    //            (mthd.Attributes.HasFlag MethodAttributes.Static)
    //            InstanceMethod
    //            StaticMethod
    //    | _ -> UnknownMember info.Name
