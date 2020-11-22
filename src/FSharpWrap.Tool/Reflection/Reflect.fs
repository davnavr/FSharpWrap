﻿[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Reflect

open System.IO
open System.Reflection

open FSharpWrap.Tool

let private context r ldf filter =
    using
        (new MetadataLoadContext(r))
        (fun data ->
            let ctx = Context.init filter
            ldf data
            |> Seq.map
                (fun (assm: Assembly) ->
                    let skip =
                        let file = Path.GetFileName assm.Location
                        //Set.exists
                        //    ((=) file)
                        //    ctx.Filter.ExcludeAssemblyNames
                        false
                    if skip
                    then None
                    else
                        AssemblyInfo.ofAssembly assm ctx |> Some)
            |> Seq.choose id
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
