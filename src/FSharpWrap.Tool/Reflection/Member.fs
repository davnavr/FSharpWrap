[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Member

open System
open System.Reflection

let fsname (m: Member) =
    let bname =
        match m with
        | Constructor ctor ->
            "create" // TODO: Have special name for constructor depending on and number of type of arguments (ex: ofString for .ctor(System.String))
        | InstanceField field
        | StaticField field -> field.FieldName
        | InstanceMethod mthd
        | StaticMethod mthd -> mthd.MethodName
        | InstanceProperty prop
        | StaticProperty prop -> prop.PropName
        | UnknownMember name -> name
        |> String.mapi
            (function
            | 0 -> Char.ToLowerInvariant
            | _ -> id)
    bname

let ofInfo (info: MemberInfo) =
    let membert cond inst stat =
        if cond then stat else inst
    let mparams (m: #MethodBase) =
        m.GetParameters()
        |> Seq.map (fun pinfo ->
            { ArgType = TypeRef.ofType pinfo.ParameterType |> TypeArg
              ParamName = SimpleName.ofParameter pinfo })
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
    | :? PropertyInfo as prop ->
        { PropName = prop.Name
          Setter = prop.CanRead
          PropType = TypeRef.ofType prop.PropertyType }
        |> membert
             ((prop.GetAccessors() |> Array.head).Attributes.HasFlag MethodAttributes.Static)
             InstanceProperty
             StaticProperty
    | :? MethodInfo as mthd ->
        { MethodName = mthd.Name
          Params = mparams mthd
          RetType = TypeRef.ofType mthd.ReturnType
          TypeParams = [] } // TODO: Type parameters.
        |> membert
            (mthd.Attributes.HasFlag MethodAttributes.Static)
            InstanceMethod
            StaticMethod
    | _ -> UnknownMember info.Name
