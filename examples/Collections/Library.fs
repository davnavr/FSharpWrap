module Collections.Library

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel

let hello() =
    let greet = Action<string>(printfn "Hello %s!")
    let names = ImmutableList.CreateRange [ "Bob"; "Alice"; "AbstractItemFactoryProxyFactoryManager" ]

    // Call functions without needing parenthesis and commas
    ImmutableList.forEach greet names

    // Curried functions
    names
    |> IEnumerable.getEnumerator
    |> printfn "%A"

    // Pattern matching on boolean properties
    match names with
    | ImmutableList.IsEmpty -> printfn "Empty!"
    | notEmpty -> printfn "%O" notEmpty

    // Constructor calls
    let names' = ReadOnlyCollection.ofIList names

    // Get-only properties
    names'
    |> ReadOnlyCollection.count
    |> printfn "%i"

    // Computation Expressions
    let nums: ImmutableList<int> = // TODO: Fix and make sure it has the correct types
        ImmutableList.expr {
            3
            1
            4
            1
            5
        }
    ()
