namespace rec FSharpWrap.Tool.Reflection

open System
open System.Collections.ObjectModel
open System.Reflection

open FSharpWrap.Tool

[<RequireQualifiedAccess>]
module private Attributes =
    let private argType t ctx =
        Type.ref t ctx
        |> Option.defaultWith
            (fun() -> invalidOp "Type parameters as attribute arguments are not supported")

    let private argValue (value: obj) ctx =
        match value with
        | null -> AttributeArg.Null
        | :? bool as b -> AttributeArg.Bool b
        | :? char as c -> AttributeArg.Char c
        | :? Double as d -> AttributeArg.Double d
        | :? int8 as i -> AttributeArg.Int8 i
        | :? int16 as i -> AttributeArg.Int16 i
        | :? int32 as i -> AttributeArg.Int32 i
        | :? int64 as i -> AttributeArg.Int64 i
        | :? ReadOnlyCollection<CustomAttributeTypedArgument> as items ->
            Seq.map
                (fun item -> argument item ctx)
                items
            |> List.ofSeq
            |> AttributeArg.Array
        | :? Single as f -> AttributeArg.Single f
        | :? string as str -> AttributeArg.String str
        | :? Type as t -> argType t ctx |> AttributeArg.Type
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

    let argument (arg: CustomAttributeTypedArgument) ctx =
        let t = argType arg.ArgumentType ctx
        let value = argValue arg.Value ctx
        t, value

    let private create (data: CustomAttributeData) ctx =
        let attrType = Type.name data.AttributeType ctx
        let ctorArgs =
            Seq.map
                (fun arg -> argument arg ctx)
                data.ConstructorArguments
        let namedArgs =
            Seq.map
                (fun (narg: CustomAttributeNamedArgument) ->
                    let value = argument narg.TypedValue ctx
                    FsName narg.MemberName, value)
                data.NamedArguments
        { AttributeType =
            Option.defaultWith
                (fun() -> invalidOp "Invalid attribute type")
                attrType
          ConstructorArgs = List.ofSeq ctorArgs
          NamedArgs = Map.ofSeq namedArgs }

    let ofMember (mber: MemberInfo) ctx =
        mber.CustomAttributes
        |> Seq.map (fun attr -> create attr ctx)
        |> List.ofSeq

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
    let private typeName (GenericArgs gargs as t) ctx =
        let parent = Option.ofObj t.DeclaringType
        let parent' = Option.bind (fun p -> name p ctx) parent
        let gargs' =
            match parent with
            | None -> Seq.ofArray gargs
            | Some (GenericArgs inherited) ->
                Seq.where
                    (fun garg -> Array.contains garg inherited |> not)
                    gargs
            |> Seq.map (fun garg -> arg garg ctx)
            |> TypeArgList.ofSeq
        { Name = FsName.ofType t
          Namespace = Namespace.ofStr t.Namespace
          Parent = parent'
          TypeArgs = gargs' }

    let private typeRef t ctx =
        match t with
        | IsArray elem ->
            {| ElementType = arg elem ctx
               Rank = t.GetArrayRank() |> uint |}
            |> ArrayType
        | IsByRef tref -> arg tref ctx |> ByRefType
        | IsPointer pnt -> arg pnt ctx |> PointerType
        | _ -> typeName t ctx |> TypeName

    let ref t ctx =
        match arg t ctx with
        | TypeArg tref -> Some tref
        | _ -> None

    let name t ctx =
        match ref t ctx with
        | Some(TypeName tname) -> Some tname
        | _ -> None

    let arg t ctx =
        match t, ctx with
        | HasType existing -> existing
        | GenericParam constraints as gen, _ ->
            let param =
                { Constraints = GenericConstraints.empty()
                  ParamName = FsName gen.Name }
            ctx.TypeParams.Add(gen, param)
            let constraints' =
                Array.map
                    (fun ct -> arg ct ctx |> TypeConstraint)
                    constraints
                |> Set.ofArray
            GenericConstraints.update param.Constraints constraints'
            TypeParam param
        | _ ->
            let tref = typeRef t ctx
            ctx.TypeRefs.Add(t, tref)
            TypeArg tref

    let def t ctx =
        let tname =
            Option.defaultWith
                (fun() -> sprintf "Cannot create definition for type %O" t |> invalidOp)
                (name t ctx)
        let members =
            t.GetMembers()
            |> Seq.ofArray
            |> Seq.where (fun m -> m.DeclaringType = t)
            |> Seq.choose
                (function
                | FSharpComputationExpressionMemberW _
                | IsCompilerGenerated
                | IsObsoleteError
                | IsSpecialName
                | PropAccessor -> None
                | mber -> Some mber)
            |> Seq.map (fun mber -> Member.ofInfo mber ctx)
            |> List.ofSeq
        let attrs = Attributes.ofMember t ctx
        { Attributes = attrs
          Members = members
          TypeName = tname }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module AssemblyInfo =
    let ofAssembly (assm: Assembly) (ctx: Context) =
        let types =
            Seq.where
                (Filter.typeIncluded ctx.Filter)
                assm.ExportedTypes
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
            |> Seq.map (fun t -> Type.def t ctx)
            |> Seq.toList
        { FullName = assm.FullName
          Types = types }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Member =
    let private getType (info: MemberInfo) ctx =
        let membertype cond inst stat =
            if cond then stat else inst
        let mthdparams (m: MethodBase) =
            m.GetParameters()
            |> List.ofArray
            |> List.map
                (fun pinfo ->
                    let argtype = Type.arg pinfo.ParameterType ctx
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
                      ParamName = FsName.ofParameter pinfo })

        match info with
        | :? ConstructorInfo as ctor -> mthdparams ctor |> Constructor
        | :? FieldInfo as field ->
            let ftype = Type.arg field.FieldType ctx
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
        | :? PropertyInfo as prop when prop.GetIndexParameters() |> Array.isEmpty ->
            let ptype = Type.arg prop.PropertyType ctx
            { PropName = prop.Name
              Setter = prop.CanWrite
              PropType = ptype }
            |> membertype
                ((prop.GetAccessors() |> Array.head).Attributes.HasFlag MethodAttributes.Static)
                InstanceProperty
                StaticProperty
        | :? MethodInfo as mthd ->
            let paramts = mthdparams mthd
            let ret = Type.arg mthd.ReturnType ctx
            let targs = // TODO: Factor out common code for retrieving generic argument information.
                mthd.GetGenericArguments()
                |> Array.map (fun garg -> Type.arg garg ctx)
            { MethodName = mthd.Name
              Params = paramts
              RetType = ret
              TypeArgs = TypeArgList.ofArray targs }
            |> membertype
                (mthd.Attributes.HasFlag MethodAttributes.Static)
                InstanceMethod
                StaticMethod
        | _ -> UnknownMember info.Name

    let ofInfo (mber: MemberInfo) ctx =
        let mtype = getType mber ctx
        let attrs = Attributes.ofMember mber ctx
        { Attributes = attrs
          Member.Type = mtype }
