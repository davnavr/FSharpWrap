[<RequireQualifiedAccess>]
module rec FSharpWrap.Tool.Generation.Print

open System
open System.IO

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

let private indented out =
    let mutable indented = false
    { out with
        Write =
            fun str ->
                if not indented then
                    indented <- true
                    out.Write "    "
                out.Write str
        Line =
            fun() ->
                indented <- false
                out.Line() }

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
    | ByRefType tref ->
        typeArg tref |> sprintf "%s ref"
    | FsFuncType(param, ret) ->
        sprintf
            "(%s -> %s)"
            (typeArg param)
            (typeArg ret)
    | InferredType -> "_"
    | PointerType (TypeArg (IsNamedType "System" "Void" _)) -> "voidptr"
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

let attributes out (attrs: seq<GenAttribute>) =
    Seq.iter
        (fun (attr: GenAttribute) ->
            out.Write "[<"
            typeName attr.AttributeType |> out.Write
            out.Write "("
            String.concat "," attr.Arguments |> out.Write
            out.Write ")>]"
            out.Line())
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

let genBinding out (binding: GenBinding) =
    attributes out binding.Attributes
    match binding with
    | GenActivePattern pattern ->
        out.Write "let inline "
        sprintf
            "(|%s|_|)%s= %s"
            (fsname pattern.PatternName)
            (parameters pattern.Parameters)
            pattern.Body
        |> out.Write
        out.Line()
    | GenBuilder ce ->
        let name' = fsname ce.Name
        out.Write "type "
        out.Write name'
        out.Write "()="
        out.Line()
        let out' = indented out
        for op in ce.Operations do
            out'.Write "member inline _."
            match op with
            | Combine combine ->
                out'.Write "Combine(one:"
                typeArg combine.One |> out'.Write
                out'.Write ",two:"
                typeArg combine.Two |> out'.Write
                out'.Write ")="
                combine.Combine "one" "two" |> out'.Write
            | Delay ret ->
                out'.Write "Delay(f): "
                typeArg ret |> out'.Write
                out'.Write " =f()"
            | Run(t, empty) ->
                out'.Write "Run(f:"
                typeArg t |> out'.Write
                out'.Write ")=f ("
                out'.Write empty
                out'.Write ")"
            | Using u ->
                out'.Write "Using(resource,body: _ -> "
                typeArg u.Type |> out'.Write
                out'.Write ")= "
                u.Using "resource" "body" |> out'.Write
            | Yield yld ->
                out'.Write "Yield"
                if yld.From then out'.Write "From"
                out'.Write "(item:"
                typeArg yld.Item |> out'.Write
                out'.Write ")= "
                yld.Yield "item" |> out'.Write
            | Zero t ->
                out'.Write "Zero(): _ -> "
                typeArg t |> out'.Write
                out'.Write " =id"
            | Custom str ->
                out'.Write str
            out'.Line()
        out.Write "let expr = new "
        out.Write name'
        out.Write "()"
        out.Line()
    | GenFunction func ->
        out.Write "let inline "
        fsname func.Name |> out.Write
        parameters func.Parameters |> out.Write
        out.Write "= "
        out.Write func.Body
        out.Line()

let genModule out (mdle: GenModule) =
    attributes out mdle.Attributes
    out.Write "module internal "
    fsname mdle.ModuleName |> out.Write
    out.Write " ="
    out.Line()
    let out' = indented out
    out'.Write "begin"
    out'.Line()
    Set.iter
        (indented out' |> genBinding)
        mdle.Bindings
    out'.Write "end"
    out'.Line()

let genFile (file: GenFile) out =
    for line in file.Header do
        out.Write "// "
        out.Write line
        out.Line()

    if List.isEmpty file.IgnoredWarnings |> not then
        out.Write "#nowarn"
        for warn in file.IgnoredWarnings do
            sprintf " \"%i\"" warn |> out.Write
        out.Line()

    Map.iter
        (fun ns mdles ->
            out.Write "namespace "
            Print.ns ns |> out.Write
            out.Line()

            Map.iter
                (fun _ -> indented out |> genModule)
                mdles)
        file.Namespaces
