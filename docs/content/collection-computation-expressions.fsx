(*** hide ***)
(*
index=4
*)
#r "../../packages/documentation/System.Collections.Immutable/lib/netstandard2.0/System.Collections.Immutable.dll"
#load "../content/output.autogen.fs"
(**
# Collection Computation Expressions

FSharpWrap automatically generates [computation expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions)
for immutable and mutable collection types that implement [`IEnumerable<'T>`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1).

## Conditionally Generated Methods

These methods are only generated for the computation expression type when a
specific member is defined on the collection type.

### `Yield` method
In order for a generated computation expression to be usable, collection types
need at least an `Add`, `Push`, or `Enqueue` method that takes onle one
parameters with a return type either equal to the type of the collection or
`void`.

For dictionary types, an `Add` method that takes two parameters instead of one
is also allowed.
*)
open System.Collections.Immutable
open System.Collections.Generic

// Mutable type
let a =
    List.expr {
        0 // Uses `Add` method under the hood
        1
        2
    }
    |> Array.ofSeq // Allow the items to be seen in the output

let b =
    ImmutableList.expr {
        3 // Uses an immutable `Add` method under the hood
        4
        5
    }
    |> Array.ofSeq

let c =
    ImmutableDictionary.expr {
        yield "hello", "world" // Uses an `Add` method with two parameters
        yield "triglycerides", "fatty acid"
        yield "key", "value"
    }
    |> Array.ofSeq
(*** include-fsi-output ***)
(**
### `YieldFrom` method
Only defined when the collection type defines an `AddRange` method that takes
only one parameter.
*)
open System.Collections.Immutable

let d =
    ImmutableList.expr {
        yield! b
    }
    |> Array.ofSeq
(*** include-fsi-output ***)
(**
### `Zero` method
Defined when the collection type defines a parameterless constructor or defines
a static field named `Empty` with a type that is the same as the collection type.
*)
open System.Collections.Immutable

let e =
    ImmutableList.expr {
        if 1 = 0 then
            yield 1
        // `Empty` field is being used here
    }
    |> Array.ofSeq
(*** include-fsi-output ***)
(**
## Always Generated Methods

These methods are always generated no matter what
members are defined on the collection type.

## `For` method

Allows the use of [`for` loops](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/loops-for-to-expression)
inside the computation expression.
*)
open System

let f =
    ImmutableList.expr {
        for i = 97 to 102 do char i
    }
    |> String.Concat
(*** include-fsi-output ***)
(**
`TryFinally` method

Allows the use of a [`try...finally` expression](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/exception-handling/the-try-finally-expression)
within the computation expression.
*)
open System.Collections.Immutable

let g =
    ImmutableList.expr {
        try
            1
            2
            3
        finally
            printfn "Hello"
    }
    |> Array.ofSeq
(*** include-fsi-output ***)
(**
`Using` method

Allows the use of [`use` bindings](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/resource-management-the-use-keyword#use-binding)
within the computation expression.
*)
open System
open System.IO
open System.Collections.Immutable

let h =
    ImmutableList.expr {
        use stream = new MemoryStream(16)
        stream.Write(Array.replicate 16 5uy, 0, 16)
        yield! stream.GetBuffer()
    }
    |> Array.ofSeq
(*** include-fsi-output ***)
