﻿[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Generate

open System
open System.Reflection

open FSharpWrap.Tool.Reflection

let inline private attr name args =
    sprintf "[<%s(%s)>]" name args

let fromType (tdef: TypeDef) =
    [
        attr
            "global.Microsoft.FSharp.Core.CompilationRepresentation"
            "global.Microsoft.FSharp.Core.CompilationRepresentationFlags.ModuleSuffix"
        sprintf "module ``%s`` =" tdef.Name
        "    let a = ()"
    ]

let fromTypes (types: Map<string option, TypeDef list>) =
    [
        for info in types do
            match info.Key with
            | None -> "global"
            | Some nsname ->
                nsname.Split('.')
                |> Seq.map (sprintf "``%s``")
                |> String.concat "."
            |> sprintf "namespace %s"

        // TODO: How to implement indent?
    ]
    //let rec writens =
    //    function
    //    | [] -> ()
    //    | (ns: string option, types) :: tail ->
    //        [
    //            match ns with
    //            | None -> "global"
    //            | Some nsname ->
    //                nsname.Split('.')
    //                |> Seq.map (sprintf "``%s``")
    //                |> String.concat "."
    //            |> sprintf "namespace %s"

    //            //writets types indent
    //        ]
    //        |> List.iter write
    //        writens tail
    //Map.iter
    //    (fun ns types ->
            
    //        ())
    //    tdict

let fromAssemblies (assms: AssemblyInfo list) =
    [
        "// This code was automatically generated by FSharpWrap"
        "// Generated code for:"
        for assm in assms do
            sprintf "// %s" assm.FullName
        // TODO: Write namespaces containing the modules.
        ""
    ]
