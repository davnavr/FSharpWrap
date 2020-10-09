module FSharpWrap.Tool.Tests.ArgumentsTests

open Expecto
open FsCheck

open FSharpWrap.Tool

type ArgumentsWithHelp =
    private
    | ArgumentsWithHelp of string list

type InvalidArgumentOptions =
    private
    | InvalidArgumentOptions of string list

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

    type ArgumentGenerators =
        static member ArgumentsWithHelp() =
            gen {
                let! assemblies =
                    gen {
                        let! opt = option "assembly-paths"
                        let! paths =
                            Gen.listOf Gen.path
                            |> Gen.resize 6
                            |> Gen.map (List.map string)
                        return
                            [
                                opt
                                yield! paths
                            ]
                    }
                let! outfile =
                    gen {
                        let! opt = option "output-file"
                        let! file = Gen.map string Gen.path
                        return [ opt; file ]
                    }
                let! args =
                    [
                        assemblies
                        outfile
                        [ "--help" ]
                    ]
                    |> Gen.shuffle
                    |> Gen.map (List.ofArray >> List.collect id)
                return ArgumentsWithHelp args
            }
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
    let parseFails reason f =
        Expect.parseError reason
        |> f
        |> argumentsProperty reason

    testList "argument parsing tests" [
        parseFails
            "fails when --help is specified"
            (fun f (ArgumentsWithHelp args) -> f args)

        parseFails
            "fails when invalid flag is specified"
            (fun f (InvalidArgumentOptions args) -> f args)
    ]
