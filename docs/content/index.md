0
# Overview

FSharpWrap generates F# functions and active patterns for your dependencies,
reducing the amount of F# code you need to write just to use code written
in other .NET languages.

## Example

```fsharp
let myList = System.Collections.Generic.List.ofSeq [| 1; 2; 3 |]
List.add myList 4 |> ignore

// Currying!
let addItem: int -> unit = List.add myList
addItem 5
```

## Tutorial

Click [here](./getting-started.html) to get started with using FSharpWrap.
