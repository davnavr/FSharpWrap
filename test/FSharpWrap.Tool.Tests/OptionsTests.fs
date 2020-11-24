module FSharpWrap.Tool.Tests.OptionsTests

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
        Gen.map
            (sprintf "--%s%s" name)
            Gen.ws

    let private arguments more t =
        gen {
            let! assemblies =
                Gen.path
                |> Gen.nonEmptyListOf
                |> Gen.resize 6
                |> Gen.map (List.map string)
            let! outfile =
                gen {
                    let! opt = option "output-file"
                    let! file = Gen.map string Gen.path
                    return [ opt; file ]
                }
            let! debug = Gen.elements [ []; [ "--launch-debugger" ] ]
            let! rest =
                [
                    outfile
                    debug
                    more
                ]
                |> Gen.shuffle
                |> Gen.map (List.ofArray >> List.collect id)
            return t (assemblies @ rest)
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
                |> Gen.filter (fun str -> Map.containsKey str Options.all |> not)
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
            (Options.parse args)
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
                    Expect.wantOk (Options.parse argv) "Parsing unexpectedly failed"
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

        successfulParse
            "parsed arguments should specify debugger launch"
            (fun argv args ->
                List.contains
                    "--launch-debugger"
                    argv
                |> Expect.equal args.LaunchDebugger)

        testCase "output file cannot be specified twice" <| fun() ->
            let result =
                [
                    "./MyAssembly.dll"
                    "--output-file"
                    "./File1.fs"
                    "--output-file"
                    "./File2.fs"
                ]
                |> Options.parse

            Expect.equal result (Error MultipleOutputFiles) "Parsing should fail because --output-file is specified more than once"

        testCase "included assemblies can be specified more than once" <| fun() ->
            let result =
                [
                    "./Hello/World.dll"
                    "--output-file"
                    "./Temp/Thing.fs"
                    "--include-assembly-names"
                    "FancyParsing"
                    "Keyboard"
                    "My.Thing"
                    "--include-assembly-names"
                    "CI"
                ]
                |> Options.parse
            Expect.isOk result "Parsing should succeed with options expecting lists"

        testCase "cannot have mixed filter for assembly names" <| fun() ->
            let result =
                [
                    "./FSharpWrap.Tool.dll"
                    "--output-file"
                    "./fun.fs"
                    "--include-assembly-names"
                    "One"
                    "--exclude-assembly-names"
                    "Two"
                ]
                |> Options.parse
            Expect.equal result (Error MixedFilter) "Parsing should fail because assembly file filter is mixed"
    ]
