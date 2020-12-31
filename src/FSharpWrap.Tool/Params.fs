namespace FSharpWrap.Tool

open System
open System.Collections.Generic
open System.Reflection

type Param = FsName * Type

type Params = Param[]

[<RequireQualifiedAccess>]
module Params =
    let create (parr: ParameterInfo[]) =
        let parr' = Dictionary<FsName, Type> parr.Length
        Array.map
            (fun p ->
                let pname = FsName.ofParameter p
                let pname' =
                    let mutable name = pname
                    while parr'.ContainsKey name do
                        name <- FsName.append "'" name
                    name
                pname', p.ParameterType)
            parr

    let inline ofCtor (ctor: ConstructorInfo) = ctor.GetParameters() |> create
