[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Member

open System
open System.Reflection

let fsname (m: Member) =
    let name =
        String.mapi
            (function
            | 0 -> Char.ToLowerInvariant
            | _ -> id)
            m.Name
    name

let ofInfo (info: MemberInfo) =
    let membert cond inst stat =
        if cond then stat >> StaticMember else inst >> InstanceMember
    match info with
    | :? FieldInfo as field ->
        { Name = field.Name
          FieldType = TypeRef.ofType field.FieldType }
        |> membert
            (field.Attributes.HasFlag FieldAttributes.Static)
            InstanceField
            StaticField
    | :? PropertyInfo as prop ->
        { Name = prop.Name
          Setter = prop.CanRead
          PropType = TypeRef.ofType prop.PropertyType }
        |> membert
             ((prop.GetAccessors() |> Array.head).Attributes.HasFlag MethodAttributes.Static)
             InstanceProperty
             StaticProperty
    | :? MethodInfo as mthd ->
        { Name = mthd.Name
          Parameters =
            mthd.GetParameters()
            |> Seq.map (fun pinfo -> TypeRef.ofType pinfo.ParameterType)
            |> List.ofSeq
          RetType = TypeRef.ofType mthd.ReturnType
          TypeParams = invalidOp "type params" }
        |> membert
            (mthd.Attributes.HasFlag MethodAttributes.Static)
            InstanceMethod
            StaticMethod
    | _ -> UnknownMember info.Name
