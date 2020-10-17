[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeRef

open System

let rec fsname =
    function
    | ArrayType arr ->
        match arr.Rank with
        | 0u | 1u -> ""
        | _ -> String(',', arr.Rank - 1u |> int)
        |> sprintf
            "%s[%s]"
            (TypeArg.fsname fsname arr.ElementType)
    | TypeName tname -> TypeName.print tname

let rec ofType (t: Type) =
    let ofType' = ofType >> TypeArg
    match t with
    | IsArray elem ->
        {| ElementType = ofType' elem
           Rank = t.GetArrayRank() |> uint |}
        |> ArrayType
    | _ ->
        TypeName.ofType
            (fun parent targs ->
                let inherited =
                    parent
                    |> Option.map (|GenericArgs|)
                    |> Option.defaultValue Array.empty
                targs
                |> List.ofArray
                |> List.except inherited
                |> List.map
                    (function
                    | GenericParam -> TypeParam
                    | GenericArg as targ -> ofType' targ)
                |> TypeArgList.ofList)
            t
        |> TypeName
