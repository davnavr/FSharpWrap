[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeInfo

let ofType (t: System.Type) =
    { Info = TypeRef.ofType t
      Members =
        t.GetMembers()
        |> Seq.ofArray
        |> Seq.where (fun m -> m.DeclaringType = t)
        |> Seq.map Member.ofInfo
        |> List.ofSeq }
