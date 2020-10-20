namespace rec FSharpWrap.Tool.Reflection

open System.Reflection

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TypeName =
    let rec ofType (GenericArgs gargs as t) =
        let parent = Option.ofObj t.DeclaringType
        { Name = FsName.ofType t
          Namespace = Namespace.ofStr t.Namespace
          Parent = Option.map ofType parent
          TypeArgs =
            let inherited =
                parent
                |> Option.map (|GenericArgs|)
                |> Option.defaultValue Array.empty
            gargs
            |> List.ofArray
            |> List.except inherited
            |> List.map TypeArg.ofType
            |> TypeArgList.ofList }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TypeRef =
    let rec ofType t =
        match t with
        | IsArray elem ->
            {| ElementType = TypeArg.ofType elem
               Rank = t.GetArrayRank() |> uint |}
            |> ArrayType
        | _ ->
            TypeName.ofType t |> TypeName

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TypeArg =
    let ofType =
        function
        | IsByRef _
        | IsPointer _ -> Inferred
        | GenericParam as gen -> TypeParam { TypeParam.Name = FsName gen.Name }
        | t -> TypeRef.ofType t |> TypeArg

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Member =
    let ofInfo (info: MemberInfo) =
        let membert cond inst stat =
            if cond then stat else inst
        let mparams (m: #MethodBase) =
            m.GetParameters()
            |> Seq.map (fun pinfo ->
                { ArgType = TypeArg.ofType pinfo.ParameterType
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
            |> List.ofSeq
        match info with
        | :? ConstructorInfo as ctor ->
            mparams ctor |> Constructor
        | :? FieldInfo as field ->
            { FieldName = field.Name
              FieldType = TypeRef.ofType field.FieldType
              IsReadOnly =
                if field.Attributes.HasFlag FieldAttributes.InitOnly
                then ReadOnly
                else Mutable }
            |> membert
                (field.Attributes.HasFlag FieldAttributes.Static)
                InstanceField
                StaticField
        | :? PropertyInfo as prop when prop.GetIndexParameters() |> Array.isEmpty ->
            { PropName = prop.Name
              Setter = prop.CanRead
              PropType = TypeRef.ofType prop.PropertyType }
            |> membert
                 ((prop.GetAccessors() |> Array.head).Attributes.HasFlag MethodAttributes.Static)
                 InstanceProperty
                 StaticProperty
        | :? MethodInfo as mthd ->
            { MethodName = mthd.Name, 0u // TODO: Type parameters.
              Params = mparams mthd
              RetType = TypeRef.ofType mthd.ReturnType }
            |> membert
                (mthd.Attributes.HasFlag MethodAttributes.Static)
                InstanceMethod
                StaticMethod
        | _ -> UnknownMember info.Name

// TODO: Move TypeInfo and AssemblyInfo modules here.
