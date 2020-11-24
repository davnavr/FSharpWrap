[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Reflect

open System.IO
open System.Reflection

open FSharpWrap.Tool

let private context r ldf filter =
    let included = Filter.assemblyIncluded filter
    using
        (new MetadataLoadContext(r))
        (fun data ->
            let ctx = Context.init filter
            ldf data
            |> Seq.where included
            |> Seq.map
                (fun (assm: Assembly) -> AssemblyInfo.ofAssembly assm ctx)
            |> List.ofSeq)

let paths (assms: seq<Path>) =
    let files =
        Seq.collect
            (function
            | Directory dir ->
                Directory.EnumerateFiles(dir.Path, "*.dll", SearchOption.AllDirectories)
            | File file -> Seq.singleton file.Path)
            assms
    context
        (PathAssemblyResolver files)
        (fun ctx ->
            Seq.map
                ctx.LoadFromAssemblyPath
                files)
