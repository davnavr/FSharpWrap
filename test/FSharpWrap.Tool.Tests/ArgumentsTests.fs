module FSharpWrap.Tool.Tests.ArgumentsTests

open Expecto
open FsCheck

open FSharpWrap.Tool

type ArgumentsWithHelp =
    private
    | ArgumentsWithHelp of string list

type ArgumentGenerators =
    static member ArgumentsWithHelp() =
        let ws =
            gen {
                let! len = Gen.choose(0, 10)
                return String.replicate len " "
            }
        let option name =
            gen {
                let! ws1, ws2 = Gen.two ws
                return sprintf
                    "--%s%s%s"
                    ws1
                    name
                    ws2
            }
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

[<Tests>]
let valid =
    let argumentsProperty =
        { FsCheckConfig.defaultConfig with
            arbitrary = typeof<ArgumentGenerators> :: FsCheckConfig.defaultConfig.arbitrary }
        |> testPropertyWithConfig

    testList "argument parsing tests" [
        argumentsProperty "arguments not returned when help flag is specified" <| fun (ArgumentsWithHelp args) ->
            Expect.isError
                (Arguments.parse args)
                "Arguments were successfully parsed even though --help was specified"
    ]
