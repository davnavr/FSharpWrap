[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.AssemblyInfo

open System.Reflection

let ofAssembly (assm: Assembly) =
    { FullName = assm.FullName
      Types =
        Seq.map
            TypeInfo.ofType
            assm.ExportedTypes
        |> List.ofSeq }
