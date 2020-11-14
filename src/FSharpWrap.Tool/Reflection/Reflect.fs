[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Reflect

open System.IO
open System.Reflection

open FSharpWrap.Tool

let private context r ldf filter: seq<AssemblyInfo> =
    using
        (new MetadataLoadContext(r))
        (fun data ->
            ldf data
            |> Seq.mapFold
                (fun (ctx: Context) (assm: Assembly) ->
                    let skip =
                        let file = Path.GetFileName assm.Location
                        Set.exists
                            ((=) file)
                            ctx.Filter.AssemblyFiles
                    if skip
                    then None, ctx
                    else
                        let assm', ctx' = AssemblyInfo.ofAssembly assm ctx
                        Some assm', ctx')
                (Context.init filter)
            |> fst
            |> Seq.choose id)

let paths (assms: seq<Path>) =
    let paths = Seq.map string assms
    context
        (PathAssemblyResolver paths)
        (fun ctx ->
            Seq.map
                ctx.LoadFromAssemblyPath
                paths)
