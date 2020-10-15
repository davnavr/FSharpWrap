[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeInfo

let ofType (t: System.Type) =
    { Info =
        // TODO: Factor out common code for calling TypeName.ofType
        TypeName.ofType (TypeRef.targs (TypeRef.ofType >> TypeArg)) t
      Members =
        t.GetMembers()
        |> Seq.ofArray
        |> Seq.where (fun m -> m.DeclaringType = t)
        |> Seq.choose
            (function
            | IsSpecialName
            | PropAccessor -> None
            | mber -> Some mber)
        |> Seq.map Member.ofInfo
        |> List.ofSeq }
