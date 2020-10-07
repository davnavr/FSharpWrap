module FSharpWrap.Tool.Tests.GenerateTests

open System.Reflection

open Expecto
open FsCheck

open FSharpWrap.Tool

type AssemblyGen() =
    static member Assembly(): Arbitrary<Assembly> =
        Gen.elements
            [
                typeof<TestsAttribute>.Assembly
                typeof<MetadataLoadContext>.Assembly
                typeof<PortableExecutable.PEReader>.Assembly
            ]
        |> Arb.fromGen

let private config = { FsCheckConfig.defaultConfig with arbitrary = [ typeof<AssemblyGen> ] }

[<Tests>]
let t =
    testList "code generation" [
        testPropertyWithConfig config "module exists for each public type" <| fun assm ->
            // TODO: Consider using the F# compiler services to determine that the generated code contains what is expected and is syntactically correct.
            let gen =
                Generate.fromAssemblies
                    (fun str st -> str :: st)
                    []
                    [ assm ]
                |> List.rev
            assm.DefinedTypes
            |> List.ofSeq
            |> List.iter
                (fun (t: TypeInfo) ->
                    let result =
                        List.tryFind
                            (fun (str: string) ->
                                sprintf "module ``%s" t.Name |> str.StartsWith)
                            gen
                        |> Option.map Ok
                        |> Option.defaultValue (Error t)
                    Expect.isOk result "public type has module")
    ]
