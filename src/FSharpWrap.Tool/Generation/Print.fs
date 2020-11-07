[<RequireQualifiedAccess>]
module internal rec FSharpWrap.Tool.Generation.Print

open System

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

let indented lines = Seq.map (sprintf "    %s") lines

let block lines =
    seq {
        yield "begin"
        yield! indented lines
        yield "end"
    }

let fsname (FsName name) = sprintf "``%s``" name

let ns (Namespace strs) =
    match strs with
    | [] -> "global"
    | _ ->
        strs
        |> List.map fsname
        |> String.concat "."

let typeArg =
    function
    | TypeArg targ -> typeRef targ
    | TypeParam tparam -> fsname tparam.ParamName |> sprintf "'%s"

let paramType { Param.ArgType = argt } =
    match argt with
    | TypeArg targ -> typeRef targ
    | TypeParam tparam ->
        let tname = fsname tparam.ParamName |> sprintf "'%s"
        match tparam with
        | HasGenericConstraints constraints ->
            Seq.map
                (fun constr ->
                    let (TypeConstraint derives) = constr
                    typeArg derives |> sprintf "%s:>%s" tname)
                constraints
            |> String.concat " and "
            |> sprintf " when %s"
        | _ -> ""
        |> sprintf "%s%s" tname

let typeRef =
    function
    | ArrayType arr ->
        let rank =
            match arr.Rank with
            | 0u | 1u -> ""
            | _ -> String(',', arr.Rank - 1u |> int)
        sprintf
            "%s[%s]"
            (typeArg arr.ElementType)
            rank
    | ByRefType _ -> "_"
    | PointerType pnt ->
        typeArg pnt |> sprintf "nativeptr<%s>"
    | TypeName tname -> typeName tname

let typeName { Name = name; Namespace = nspace; Parent = parent; TypeArgs = TypeArgs targs } =
    let name' =
        let targs' =
            match targs with
            | [] -> ""
            | _ ->
                List.map typeArg targs
                |> String.concat ","
                |> sprintf "<%s>"
        sprintf
            "%s%s"
            (fsname name)
            targs'
    match parent with
    | None ->
        sprintf "%s.%s" (ns nspace)
    | Some parent' ->
        sprintf "%s.%s" (typeName parent')
    <| name'

// TODO: How will generic methods and static fields of generic types be handled, maybe add [<GeneralizableValue>]?
let memberName mber =
    match mber.Type with
    | Constructor cparams ->
        let ptypes =
            List.map
                (fun pt ->
                    match pt.ArgType with
                    | TypeArg targ -> Some targ
                    | _ -> None)
                cparams
        match ptypes with
        | [ Some (IsNamedType "System.Collections.Generic" "IEnumerable" _ & TypeName { TypeArgs = TypeArgs [ _ ] }) ] ->
            "ofSeq"
        | [ Some (IsNamedType "Microsoft.FSharp.Collections" "List" _ & TypeName { TypeArgs = TypeArgs [ _ ] }) ] ->
            "ofList"
        | [ Some (ArrayType _) ] ->
            "ofArray"
        | [ Some (TypeName { Name = pname }) ] -> sprintf "of%O" pname
        | _ -> "create"
    | InstanceField field
    | StaticField field -> field.FieldName
    | InstanceMethod mthd
    | StaticMethod mthd -> mthd.MethodName
    | InstanceProperty prop
    | StaticProperty prop -> prop.PropName
    | UnknownMember name -> name
    |> String.toCamelCase

let arguments parameters =
    Seq.map
        (fun (pname, param) ->
            let name = fsname param.ParamName
            let f =
                match param.IsOptional with
                | FsOptionalParam -> sprintf "?%s=%s"
                | OptionalParam -> sprintf "%s=%s"
                | RequiredParam -> fun _ -> id
            fsname pname |> f name)
        parameters
    |> String.concat ","

let attributes (attrs: seq<GenAttribute>) =
    Seq.map
        (fun (attr: GenAttribute) ->
            let name = typeName attr.AttributeType
            let args = String.concat "," attr.Arguments
            sprintf "[<%s(%s)>]" name args)
        attrs

let parameters =
    function
    | EmptyParams -> "()"
    | ParamList plist ->
        List.map
            (fun (pname, param) -> 
                sprintf
                    "(%s:%s)"
                    (Print.fsname pname)
                    (paramType param))
            plist
        |> String.Concat

let genBinding binding =
    let binding' =
        match binding with
        | GenActivePattern pattern ->
            sprintf
                "let inline (|%s|_|)%s= %s"
                (fsname pattern.PatternName)
                (parameters pattern.Parameters)
                pattern.Body
        | GenFunction func ->
            sprintf
                "let inline %s%s= %s"
                (fsname func.Name)
                (parameters func.Parameters)
                func.Body
    seq {
        yield! attributes binding.Attributes
        binding'
    }

let genModule (mdle: GenModule) =
    seq {
        yield! attributes mdle.Attributes
        fsname mdle.ModuleName |> sprintf "module %s ="
        yield!
            mdle.Bindings
            |> Seq.collect genBinding
            |> block
            |> indented
    }

let genFile (file: GenFile) =
    let header = Seq.map (sprintf "// %s") file.Header
    let warnings =
        match file.IgnoredWarnings with
        | [] -> ""
        | _ ->
            file.IgnoredWarnings
            |> List.map (sprintf "\"%i\"")
            |> String.concat " "
            |> sprintf "#nowarn %s"
    let contents =
        file.Namespaces
        |> Map.toSeq
        |> Seq.collect
            (fun (ns, mdles) ->
                
                let mdles' =
                    mdles
                    |> Map.toSeq
                    |> Seq.map snd
                seq {
                    Print.ns ns |> sprintf "namespace %s"
                    yield!
                        mdles'
                        |> Seq.collect genModule
                        |> indented
                })
    seq {
        yield! header
        warnings
        yield! contents
    }
