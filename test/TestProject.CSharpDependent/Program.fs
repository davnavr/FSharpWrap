module Program

open Expecto

open System.Collections.Generic
open System.Diagnostics

open CSharpDependency

[<EntryPoint>]
let main argv =
    testList "project reference tests" [
        testCase "list expression is equivalent" <| fun() ->
            let exp = List.collect id [ [ 'a'..'z' ]; [ 'A'..'Z' ] ]
            let act =
                List.expr {
                    yield! [ 'a'..'z' ]
                    for c in [ 'A'..'Z' ] do yield c
                }
            Expect.sequenceEqual act exp "Lists should be equivalent"

        testProperty "constructor produces equal value" <| fun str ->
            let exp = MyString(str).value
            let act = MyString.ofString str |> MyString.value
            Expect.equal act exp "Two objects must be equal"

        testCase "code generated for implicitly included class" <| fun() ->
            let frame = new StackTrace()
            let exp = frame.GetFrame(0)
            Expect.equal (StackTrace.getFrame 0 frame) exp "Property values should be equal"

        testList "custom collection" [
            testProperty "ofSeq uses same items" <| fun (items: string list) ->
                Expect.sequenceEqual
                    (MyCustomList.ofSeq items)
                    items
                    "Collections should be the same"

            testProperty "computation expression works" <| fun one (rest: (string * string) list) ->
                let exp = one :: rest
                let act = MyCustomList.expr { one; yield! rest }
                Expect.sequenceEqual act exp "Both sequences should be the same"
        ]
    ]
    |> runTestsWithCLIArgs Seq.empty argv
