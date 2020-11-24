module MultiTarget.Tests

open System

let getAssembly (t: Type) =
    Type.assembly t
