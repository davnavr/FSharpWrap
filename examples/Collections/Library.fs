module Collections.Library

open System
open System.Collections.Generic
open System.Collections.Immutable

let hello() =
    let greet = Action<string>(printfn "Hello %s!")
    let names = ImmutableList.CreateRange [ "Bob"; "Alice"; "AbstractItemFactoryProxyFactoryManager" ]

    // Call functions without needing parenthesis and commas
    ImmutableList.forEach greet names

    // Curried functions
    names |> IEnumerable.getEnumerator |> printfn "%A"

    // Pattern matching on boolean properties
    match names |> ImmutableStack.Create with
    | ImmutableStack.IsEmpty -> printfn "Empty!"
    | notEmpty -> printfn "%O" notEmpty
