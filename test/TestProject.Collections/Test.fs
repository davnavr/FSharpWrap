
open FsCheck

open Expecto

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel

let hello() =
    let dictionary: ImmutableDictionary<string, string> =
        ImmutableDictionary.expr {
            yield "word", "the thing to the left"
            yield "dictionary", "what this is"
        }
    ()

type CollectionGenerator =
    static member List() =
        Arb.generate<_ list>
        |> Gen.map List<_>
        |> Arb.fromGen

    static member ImmutableList() =
        Gen.map ImmutableList.CreateRange Arb.generate<_ list> |> Arb.fromGen

[<EntryPoint>]
let main argv =
    let config =
        { FsCheckConfig.defaultConfig with
            arbitrary = typeof<CollectionGenerator> :: FsCheckConfig.defaultConfig.arbitrary }
    let inline testCollection name =
        testPropertyWithConfig config name

    testList "collection generation tests" [
        testCollection "constructor call is equal" <| fun (items: List<string>) ->
            Expect.sequenceEqual
                (ReadOnlyCollection.ofIList items)
                (ReadOnlyCollection items)
                "The collections should call the same constructor"

        testCollection "active pattern is equal" <| fun (items: ImmutableList<_>) ->
            ImmutableList.(|IsEmpty|_|) items |> Option.isSome = items.IsEmpty

        testCollection "property values are equal" <| fun (items: ImmutableList<_>) ->
            ImmutableList.count items = items.Count

        testCase "computation expressions produce equal value" <| fun() ->
            let ce =
                ImmutableList.expr {
                    3
                    1
                    4
                    1
                    5
                    yield! [ 9; 2; 6; 5 ]
                }
            let exp =
                ImmutableList.CreateRange [ 3; 1; 4; 1; 5; 9; 2; 6; 5 ]
            Expect.sequenceEqual ce exp "Both lists should contain the same elments in the same order"

        testProperty "computation expression singletons should be equal" <| fun (item: uint32) ->
            Expect.sequenceEqual
                (ImmutableList.expr { item })
                (ImmutableList.Create item)
                "Both lists should contain the same element"

        testProperty "computation expression is correct for mutable type" <| fun(items: string list) ->
            Expect.sequenceEqual
                (List.expr { yield! items })
                items
                "List should contain same items in same order as sequence"

        testCollection "adding items is equal" <| fun (initial: ImmutableList<uint64>) add ->
            let exp = initial.Add add
            let act = ImmutableList.add add initial
            Expect.sequenceEqual act exp "The lists should contain the same elements when an item is added"

        testCollection "multiple parameters" <| fun (items: ImmutableList<string>) vold vnew ->
            let exp = items.Add(vold).Replace(vold, vnew)
            let act = ImmutableList.add vold items |> ImmutableList.replace vold vnew
            Expect.sequenceEqual act exp "The lists should replace the same element"
    ]
    |> runTestsWithCLIArgs Seq.empty argv
