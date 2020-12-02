module MultiTarget.Tests

open Expecto

open System

let getAssembly (t: Type) =
    Type.assembly t

[<EntryPoint>]
let main argv =
    testList "multi-target tests" [
        testCase "assemblies are equal" <| fun() ->
            let t = typeof<obj>
            Expect.equal (Type.assembly t) t.Assembly "Generated function should return equal object"

#if NET5_0
        testCase "target framework dependent code works" <| fun() ->
            // TODO: Find a function that is only generated for .NET 5
            let target = typeof<obj>
            let t = typeof<obj list>
            Expect.equal
                (Type.isAssignableTo target t)
                (t.IsAssignableTo target)
                "Results should be equal"
#endif
    ]
    |> runTestsWithCLIArgs Seq.empty argv
