[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Reflect

open System.Reflection

open FSharpWrap.Tool

let private context r ldf =
    using
        (new MetadataLoadContext(r))
        (ldf >> List.map AssemblyInfo.ofAssembly)

let paths (assms: Path list) =
    let paths = List.map string assms
    context
        (List.toSeq paths |> PathAssemblyResolver)
        (fun ctx ->
            List.map
                ctx.LoadFromAssemblyPath
                paths)
