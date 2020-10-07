[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Reflect

open System.Reflection

let private context r ldf =
    using
        (new MetadataLoadContext(r))
        (ldf >> List.map AssemblyInfo.ofAssembly)

let paths assms =
    context
        (List.toSeq assms |> PathAssemblyResolver)
        (fun ctx ->
            List.map
                ctx.LoadFromAssemblyPath
                assms)
