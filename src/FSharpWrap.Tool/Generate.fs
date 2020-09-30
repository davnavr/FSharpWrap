[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Generate

open System
open System.Reflection

open FSharpWrap.Tool.Reflection

let private indent write str = sprintf "    %s" str |> write

let inline private attr name args =
    sprintf "[<%s(%s)>]" name args

let fromType write state (tdef: Type) =
    [
        attr
            "Microsoft.FSharp.Core.CompilationRepresentation"
            "Microsoft.FSharp.Core.CompilationRepresentationFlags.ModuleSuffix"
        sprintf
            "module %s ="
            (Type.simpleName tdef)
        "    let a = ()"
    ]
    |> List.fold
        (fun state' str -> write str state')
        state

let fromTypes write state (tdict: Map<string option, Type list>) =
    let rec writets write' state' =
        function
        | [] -> state'
        | h :: tail ->
            let state'' = fromType write state' h
            writets write' state'' tail
    let rec writens state' =
        function
        | [] -> state'
        | (ns: string option, types) :: tail ->
            let state'' =
                [
                    match ns with
                    | None -> "global"
                    | Some nsname ->
                        nsname.Split('.')
                        |> Seq.map (sprintf "``%s``")
                        |> String.concat "."
                    |> sprintf "namespace %s"
                    |> write

                    writets (indent write) types
                ]
                |> List.fold (fun st w -> w st) state'
            writens state'' tail
    tdict
    |> Map.toList
    |>  writens state

let fromAssemblies write state assms =
    // TODO: Consider filtering out F# assemblies from the assms list.
    let types =
        let rec inner tdict ndict =
            function
            | [], [] -> Ok ndict
            | [], (h: Assembly) :: assms' ->
                let cont = List.ofSeq h.ExportedTypes, assms'
                inner tdict ndict cont
            | (h :: types, assms') ->
                match Map.tryFind h.FullName tdict with
                | Some dup -> Error dup
                | None ->
                    let tdict' = Map.add h.FullName h tdict
                    let ndict' =
                        let ns = Option.ofObj h.Namespace
                        let siblings =
                            ndict
                            |> Map.tryFind ns
                            |> Option.defaultValue []
                        Map.add ns (h :: siblings) ndict
                    let cont = types, assms'
                    inner tdict' ndict' cont
        inner Map.empty Map.empty ([], assms)
    ()
