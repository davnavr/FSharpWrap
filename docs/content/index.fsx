(**
index=0
*)
(*** hide ***)
#r "../../packages/documentation/System.Collections.Immutable/lib/netstandard2.0/System.Collections.Immutable.dll"
#load "../content/output.autogen.fs"
(**
# Overview

FSharpWrap generates F# functions and active patterns for your dependencies,
reducing the amount of F# code you need to write just to use code written
in other .NET languages.

## Example
*)

open System.Collections.Immutable

let original = ImmutableList.CreateRange [| 5; 9; 2 |]

// Currying!
let add1 = ImmutableList.add 1
let copy = add1 original

// Computation Expressions
let example =
    ImmutableList.expr {
        3
        1
        4
        yield! copy
    }

printfn "%A" example

(*** include-output ***)
(**
## Tutorial

Click [here](./getting-started.html) to get started with using FSharpWrap.
*)
