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

## Generated Methods

### `Yield` method
In order for a generated computation expression to be usable, collection types
need at least an `Add` method that takes either one or two parameters with a return type
equal to the type of the collection or `void`.
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

let b =
    ImmutableList.expr {
        3
        4
        5
    }
(*** include-fsi-output ***)
(**
### `YieldFrom` method
*)
