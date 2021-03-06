﻿[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Program

open System.Diagnostics
open System.IO

open FSharpWrap.Tool.Reflection
open FSharpWrap.Tool.Generation

[<EntryPoint>]
let main argv =
    match List.ofArray argv |> Options.parse with
    | Ok args ->
        if args.LaunchDebugger then
            Debugger.Launch() |> ignore

        try
            let file =
                let name = File.fullPath args.OutputFile
                new StreamWriter(name.Path)
            let print =
                let assms =
                    List.map Path.fullPath args.Assemblies
                args.Filter
                |> Reflect.paths assms
                |> Generate.fromAssemblies
                |> Print.genFile
            using
                file
                (fun stream ->
                    { Close = stream.Close
                      Line = stream.WriteLine
                      Write = stream.Write }
                    |> print)
            0
        with
        | ex ->
            stderr.WriteLine ex.Message
            -1
    | Error ShowUsage ->
        printfn "Usage: fsharpwrap <assembly files> [options]"
        stdout.WriteLine()
        printfn "Assembly Files:"
        printfn "  The paths to all assemblies and their dependencies"
        stdout.WriteLine()
        printfn "Options:"
        for name, opt in Map.toSeq Options.all do
            printf "  --%s" name
            let arg =
                match opt.ArgType with
                | Options.Switch -> ""
                | Options.Argument name -> sprintf " <%s>" name
                | Options.ArgumentList names ->
                    sprintf " [%s]" names
            printfn "%s" arg
            printfn "    %s" opt.Description
        0
    | Error msg ->
        stderr.WriteLine msg
        -1
