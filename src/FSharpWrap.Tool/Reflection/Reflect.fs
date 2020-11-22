[<RequireQualifiedAccess>]
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
                        Set.exists
                            ((=) file)
                            ctx.Filter.ExcludeAssemblyNames
                    if skip
                    then None
                    else
                        AssemblyInfo.ofAssembly assm ctx |> Some)
            |> Seq.choose id
            |> List.ofSeq)

let paths (assms: seq<Path>) =
    let paths = Seq.map string assms
    context
        (PathAssemblyResolver paths)
        (fun ctx ->
            Seq.map
                ctx.LoadFromAssemblyPath
                paths)
