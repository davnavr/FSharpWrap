module Collections.Library

open System
open System.Collections.Immutable

let hello() =
    let greet = Action<string>(printfn "Hello %s!")
    let names = ImmutableList.CreateRange [ "Bob"; "Alice"; "AbstractItemFactoryProxyFactoryManager" ]
    ImmutableList.forEach greet names
