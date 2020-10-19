[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeInfo

let ofType (t: System.Type) =
    { Members =
        t.GetMembers()
        |> Seq.ofArray
        |> Seq.where (fun m -> m.DeclaringType = t)
        |> Seq.choose
            (function
            | IsCompilerGenerated
            | IsSpecialName
            | PropAccessor -> None
            | mber -> Some mber)
        |> Seq.map Member.ofInfo
        |> List.ofSeq
      TypeName = TypeName.ofType t }
