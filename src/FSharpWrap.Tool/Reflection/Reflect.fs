[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Reflect

open System.Reflection

open FSharpWrap.Tool

let private context r ldf: seq<AssemblyInfo> =
    using
        (new MetadataLoadContext(r))
        (fun data ->
            ldf data
            |> Seq.mapFold
                (fun ctx assm -> AssemblyInfo.ofAssembly assm ctx)
                Context.empty
            |> fst)

let paths (assms: seq<Path>) =
    let paths = Seq.map string assms
    context
        (PathAssemblyResolver paths)
        (fun ctx ->
            Seq.map
                ctx.LoadFromAssemblyPath
                paths)
