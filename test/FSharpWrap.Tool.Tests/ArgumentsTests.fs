﻿module FSharpWrap.Tool.Tests.ArgumentsTests

open Expecto
open FsCheck

open FSharpWrap.Tool

type ArgumentsWithHelp =
    private
    | ArgumentsWithHelp of string list

type InvalidArgumentOptions =
    private
    | InvalidArgumentOptions of string list

type ValidArguments =
    private
    | ValidArguments of string list

[<AutoOpen>]
module Generators =
    let private option name =
        gen {
            let! ws1, ws2 = Gen.two Gen.ws
            return sprintf
                "--%s%s%s"
                ws1
                name
                ws2
        }

    let assemblies =
        gen {
            let! opt = option "assembly-paths"
            let! paths =
                Gen.path
                |> Gen.nonEmptyListOf
                |> Gen.resize 6
                |> Gen.map (List.map string)
            return
                [
                    opt
                    yield! paths
                ]
        }

    let outfile =
        gen {
            let! opt = option "output-file"
            let! file = Gen.map string Gen.path
            return [ opt; file ]
        }

    let private arguments more t =
        gen {
            let! assemblies' = assemblies
            let! outfile' = outfile
            return!
                [
                    assemblies'
                    outfile'
                    more
                ]
                |> Gen.shuffle
                |> Gen.map (List.ofArray >> List.collect id >> t)
        }

    type ArgumentGenerators =
        static member ArgumentsWithHelp() =
            arguments
                [ "--help" ]
                ArgumentsWithHelp
            |> Arb.fromGen

        static member InvalidArgumentOptions() =
            let badopt =
                gen {
                    let! name =
                        [
                            [ 'a'..'z' ]
                            [ 'A'..'Z' ]
                            [ '-' ]
                        ]
                        |> List.collect id
                        |> Gen.chars
                    return! option name
                }
                |> Gen.filter (fun str -> Map.containsKey str Arguments.all |> not)
            badopt
            |> Gen.listOf
            |> Gen.map InvalidArgumentOptions
            |> Arb.fromGen

        static member ValidArguments() =
            ValidArguments
            |> arguments []
            |> Arb.fromGen

module private Expect =
    let parseError msg args =
        Expect.isError
            (Arguments.parse args)
            msg

[<Tests>]
let tests =
    let config =
        { FsCheckConfig.defaultConfig with
            arbitrary =
                typeof<ArgumentGenerators> :: FsCheckConfig.defaultConfig.arbitrary }
    let argumentsProperty name =
        testPropertyWithConfig
            config
            name
    let parsingProperty reason check expect =
        check
        >> expect reason
        |> argumentsProperty reason
    let successfulParse reason f =
        parsingProperty
            reason
            (fun (ValidArguments args) -> args)
            (fun msg argv ->
                let args =
                    Expect.wantOk (Arguments.parse argv) "Parsing unexpectedly failed"
                f argv args msg)

    testList "argument parsing tests" [
        parsingProperty
            "parsing fails when --help is specified"
            (fun (ArgumentsWithHelp args) -> args)
            Expect.parseError

        parsingProperty
            "parsing fails when invalid flag is specified"
            (fun (InvalidArgumentOptions args) -> args)
            Expect.parseError

        successfulParse
            "parsed arguments should contain validated output path"
            (fun argv args -> string args.OutputFile |> Expect.contains argv)

        successfulParse
            "parsed arguments should contain validated assembly paths"
            (fun argv args ->
                args.Assemblies
                |> List.map string
                |> Expect.containsAll argv)
    ]