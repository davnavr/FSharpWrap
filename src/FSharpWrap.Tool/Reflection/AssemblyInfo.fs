[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.AssemblyInfo

open System.Reflection

let ofAssembly (assm: Assembly) =
    { FullName = assm.FullName
      Types =
        assm.ExportedTypes
        |> Seq.choose
            (function
            | Derives "System" "Delegate" _
            | AssignableTo "Microsoft.FSharp.Core" "FSharpFunc`2" _ 
            | IsNested -> None
            | t -> Some t)
        |> Seq.map TypeInfo.ofType
        |> List.ofSeq }
