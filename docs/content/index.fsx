(*** hide ***)
(*
index=0
*)
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

let original = ImmutableList.CreateRange [| 1; 5; 9 |]

// Currying!
let add2 = ImmutableList.add 2
let copy = add2 original

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
