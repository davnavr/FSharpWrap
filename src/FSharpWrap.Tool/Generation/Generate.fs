[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Generation.Generate

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection

let private attrType ns name =
    { Name = FsName name
      Namespace = Namespace.ofStr ns
      Parent = None
      TypeArgs = TypeArgList.empty }

let private moduleAttr =
    { Arguments = [ "global.Microsoft.FSharp.Core.CompilationRepresentationFlags.ModuleSuffix" ]
      AttributeType = attrType "Microsoft.FSharp.Core" "CompilationRepresentationAttribute" }

// TODO: How will the arguments be casted to the argument type?
let attribute (attr: AttributeInfo) =
    let rec arg (t, value) =
        match value with
        | AttributeArg.Array items ->
            List.map arg items
            |> String.concat ";"
            |> sprintf "[|%s|]"
        | AttributeArg.Bool b -> sprintf "%b" b
        | AttributeArg.String str -> String.toLiteral str
        | _ -> sprintf "/* Unknown argument %A */" value
    { Arguments =
        let namedArgs =
            attr.NamedArgs
            |> Map.toList
            |> List.map
                (fun (name, info) ->
                    let name' = Print.fsname name
                    arg info |> sprintf "%s=%s" name')
        List.append
            (List.map arg attr.ConstructorArgs)
            namedArgs
      AttributeType = attr.AttributeType }

let private warnAttrs =
    let warnings =
        [
            "System", "ObsoleteAttribute"
            "Microsoft.FSharp.Core", "ExperimentalAttribute"
        ]
        |> List.map (fun name -> name ||> attrType)
        |> Set.ofList
    fun (attrs: AttributeInfo list) ->
        List.filter
            (fun attr ->
                Set.contains
                    attr.AttributeType
                    warnings)
            attrs
        |> List.map attribute

let binding parent (mber: Member) =
    let name = Print.memberName mber
    let name' = FsName name
    // TODO: Maybe assign member name here as well?
    let temp = {| Attributes = warnAttrs mber.Attributes |}
    let this =
        { ArgType = TypeName parent.TypeName |> TypeArg
          IsOptional = RequiredParam
          ParamName = FsName "this" }
    match mber.Type with
    | Constructor ctor when name.StartsWith "of" ->
        let cparams = ParamList.ofList ctor |> ParamList.toList
        {| temp with
            Body =
              sprintf
                  "new ``%s``(%s)"
                  (Print.typeName parent.TypeName)
                  (Print.arguments cparams)
            Name = name'
            Parameters =
              List.map
                 (fun param -> param.ParamName, param.ArgType)
                 cparams |}
        |> GenFunction
        |> Some
    | InstanceProperty ({ PropType = TypeArg(IsNamedType "System" "Boolean" _) } as prop) ->
        {| temp with
             Body =
               sprintf
                   "if %s.``%s`` then Some() else None"
                   (Print.fsname this.ParamName)
                   prop.PropName
             Parameters = [ this.ParamName, this.ArgType ]
             PatternName = FsName prop.PropName |}
        |> GenActivePattern
        |> Some
    | InstanceMethod mthd ->
        let mparams =
            mthd.Params
            |> ParamList.ofList
            |> ParamList.append this
        let targs =
            match mthd.TypeArgs with
            | TypeArgs(_ :: _ as targs) ->
                List.map
                    Print.typeArg
                    targs
                |> String.concat ","
                |> sprintf "<%s>"
            | _ -> ""
        let rest, this' =
            let rec inner rest =
                function
                | [] -> invalidOp "The parameter list was unexpectedly empty"
                | [ this' ] -> List.rev rest, this'
                | h :: tail -> inner (h :: rest) tail
            mparams
            |> ParamList.toList
            |> inner []
        {| temp with
             Body =
               sprintf
                   "%s.``%s``%s(%s)"
                   (Print.fsname this'.ParamName)
                   mthd.MethodName
                   targs
                   (Print.arguments rest)
             Name = name'
             Parameters =
               mparams
               |> ParamList.toList
               |> List.map
                  (fun param -> param.ParamName, param.ArgType)  |}
        |> GenFunction
        |> Some
    | _ -> None

let fromType (t: TypeDef): GenModule =
    { Attributes = moduleAttr :: (warnAttrs t.Attributes)
      Bindings =
        List.fold
            (fun bindings mber ->
                match binding t mber with
                | Some gen when Set.contains gen bindings |> not ->
                    Set.add gen bindings
                | _ -> bindings)
            Set.empty
            t.Members
      ModuleName = t.TypeName.Name }

let private addType mdles tdef =
    let { Name = name; Namespace = ns } = tdef.TypeName
    let types =
        mdles
        |> Map.tryFind ns
        |> Option.defaultValue Map.empty
    let mdle =
        let init = fromType tdef
        match Map.tryFind name types with
        | Some existing ->
            let bindings =
                 Set.union
                    existing.Bindings
                    init.Bindings
            { existing with Bindings = bindings }
        | None -> init
    match mdle.Bindings with
    | Empty -> mdles
    | _ ->
        let types' =
            Map.add
                name
                mdle
                types
        Map.add ns types' mdles

let fromAssemblies (assms: seq<AssemblyInfo>) =
    { Header =
        seq {
            "This code was automatically generated by FSharpWrap"
            "Changes made to this file will be lost when it is regenerated"
            for assm in assms do
                sprintf "- %s" assm.FullName
        }
      IgnoredWarnings = [ 44u; 57u; 64u ]
      Namespaces =
        assms
        |> Seq.collect (fun assm -> assm.Types)
        |> Seq.fold
            addType
            Map.empty }
