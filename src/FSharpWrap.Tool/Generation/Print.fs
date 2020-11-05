[<RequireQualifiedAccess>]
module internal rec FSharpWrap.Tool.Generation.Print

open System

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

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

let param { ArgType = argt; ParamName = name } =
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
    |> sprintf "(%s:%s)" (fsname name)

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
                    | TypeArg (TypeName { Name = name }) -> Some name
                    | _ -> None)
                cparams
        match ptypes with
        | [ Some pname ] -> sprintf "of%O" pname
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
        (fun param ->
            let name = fsname param.ParamName
            let f =
                match param.IsOptional with
                | FsOptionalParam -> sprintf "?%s=%s"
                | OptionalParam -> sprintf "%s=%s"
                | RequiredParam -> fun _ -> id
            f name name)
        parameters
    |> String.concat ","

let attributes (attrs: seq<GenAttribute>) =
    Seq.map
        (fun (attr: GenAttribute) ->
            let name = typeName attr.AttributeType
            let args = String.concat "," attr.Arguments
            sprintf "[<%s(%s)>]" name args)
        attrs

let argsThing =
    function
    | [] -> "()"
    | args ->
        List.map
            (fun (argn, argt) -> 
                { ArgType = argt
                  IsOptional = RequiredParam
                  ParamName = argn }
                |> param)
            args
        |> String.Concat

let genBinding binding =
    let binding' =
        match binding with
        | GenActivePattern pattern ->
            sprintf
                "let inline (|%s|_|)%s= %s"
                (fsname pattern.PatternName)
                (argsThing pattern.Parameters)
                pattern.Body
        | GenFunction func ->
            sprintf
                "let inline %s%s= %s"
                (fsname func.Name)
                (argsThing func.Parameters)
                func.Body
    seq {
        yield! attributes binding.Attributes
        yield binding'
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
    Seq.append header contents
