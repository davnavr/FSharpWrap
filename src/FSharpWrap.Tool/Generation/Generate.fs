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

let private ienumerable =
    { Name = FsName "IEnumerable"
      Namespace = Namespace.ofStr "System.Collections.Generic"
      Parent = None
      TypeArgs =
        { Constraints = GenericConstraints.Empty()
          ParamName = FsName "T" }
        |> TypeParam
        |> TypeArgList.singleton }

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
    let temp = {| Attributes = warnAttrs mber.Attributes |}
    let this =
        { ArgType = TypeName parent.TypeName |> TypeArg
          IsOptional = RequiredParam
          ParamName = FsName "this" }
    match mber.Type with
    | Constructor ctor when name.StartsWith "of" ->
        let cparams = ParamList.ofList ctor
        // TODO: Factor out common code for generating functions.
        {| temp with
            Body =
              sprintf
                  "new %s(%s)"
                  (Print.typeName parent.TypeName)
                  (Print.arguments cparams)
            Name = name'
            Parameters = cparams |}
        |> GenFunction
        |> Some
    | InstanceField ({ IsReadOnly = ReadOnly } as field) ->
        {| temp with
            Body =
              sprintf
                  "%s.``%s``:%s"
                  (Print.fsname this.ParamName)
                  field.FieldName
                  (Print.typeArg field.FieldType)
            Name = name'
            Parameters = ParamList.singleton this |}
        |> GenFunction
        |> Some
    | InstanceMethod mthd ->
        let mparams = ParamList.ofList mthd.Params
        let targs =
            match mthd.TypeArgs with
            | TypeArgs(_ :: _ as targs) ->
                List.map
                    Print.typeArg
                    targs
                |> String.concat ","
                |> sprintf "<%s>"
            | _ -> ""
        let mparams', (this', _) = ParamList.append this mparams
        {| temp with
             Body =
               sprintf
                   "%s.``%s``%s(%s)"
                   (Print.fsname this')
                   mthd.MethodName
                   targs
                   (Print.arguments mparams)
             Name = name'
             Parameters = mparams' |}
        |> GenFunction
        |> Some
    | InstanceProperty ({ PropType = TypeArg(IsNamedType "System" "Boolean" _); Setter = false } as prop) ->
        {| temp with
             Body =
               sprintf
                   "if %s.``%s`` then Some() else None"
                   (Print.fsname this.ParamName)
                   prop.PropName
             Parameters = ParamList.singleton this
             PatternName = FsName prop.PropName |}
        |> GenActivePattern
        |> Some
    | InstanceProperty ({ Setter = false } as prop) ->
        // TODO: Factor out common code shared with an InstanceField
        {| temp with
            Body =
              sprintf
                  "%s.``%s``"
                  (Print.fsname this.ParamName)
                  prop.PropName
            Name = name'
            Parameters = ParamList.singleton this |}
        |> GenFunction
        |> Some
    // TODO: Create functions to call static methods, make sure to filter out union case constructors.
    | _ -> None

let fromType (t: TypeDef): GenModule =
    let isRef =
        function
        | { Param.ArgType = TypeArg(ByRefType _) } -> true
        | _ -> false
    { Attributes = moduleAttr :: (warnAttrs t.Attributes)
      Bindings =
        /// Attempts to create a Computation Expression for the type
        let expr =
            if not t.IsAbstract && Set.contains ienumerable t.Interfaces
            then
                let tname = Print.typeName t.TypeName
                let t' = TypeName t.TypeName |> TypeArg
                let idt = FsFuncType(t', t') |> TypeArg
                let idtname = Print.typeArg idt
                let empty =
                    List.tryPick
                        (fun mber ->
                            match mber.Type with
                            | StaticField { FieldName = "Empty" } ->
                                sprintf "%s.Empty" tname |> Some
                            | Constructor [] ->
                                sprintf "new %s()" tname |> Some
                            | _ -> None)
                        t.Members
                let ops =
                    let ret (rt: TypeArg) =
                        if rt = t'
                        then System.String.Empty
                        else " |> ignore; this"
                    let others =
                        List.collect
                            (fun mber ->
                                match mber.Type with
                                | InstanceMethod({ MethodName = String.OneOf [ "Add"; "AddRange"; "Push"; "Enqueue" ]; Params = [ arg ] } as mthd) ->
                                    [
                                        { From = mthd.MethodName = "AddRange"
                                          Item = arg.ArgType
                                          Yield =
                                            fun item ->
                                                ret mthd.RetType
                                                |> sprintf
                                                    "fun (this: %s) -> this.%s(%s)%s"
                                                    tname
                                                    mthd.MethodName
                                                    item }
                                        |> Yield
                                    ]
                                | InstanceMethod({ MethodName = "Add"; Params = [ _; _ ] } as mthd) ->
                                    [
                                        { From = false
                                          Item = TypeArg InferredType
                                          Yield =
                                            fun item ->
                                                ret mthd.RetType
                                                |> sprintf
                                                    "fun (this: %s) -> this.%s(fst %s, snd %s)%s"
                                                    tname
                                                    mthd.MethodName
                                                    item
                                                    item }
                                        |> Yield
                                    ]
                                | _ -> List.empty)
                            t.Members
                    [
                        { Combine = sprintf "%s >> %s"
                          One = idt
                          Two = idt }
                        |> Combine

                        sprintf
                            "For(items: seq<_>, body): %s = fun this -> Seq.fold (fun state item -> body item state) this items"
                            idtname
                        |> Custom

                        sprintf
                            "TryFinally(body: %s, fin: unit -> unit) = fun this -> try body this finally fin()"
                            idtname
                        |> Custom

                        { Type = idt
                          Using =
                            sprintf
                                "fun (this: %s) -> this |> using %s %s"
                                tname }
                        |> Using

                        Delay idt
                        Zero t'

                        if empty.IsSome then Run(idt, empty.Value)

                        yield! others
                    ]
                    |> Set.ofList
                {| Attributes = List.empty
                   Name =
                     let (FsName name) = t.TypeName.Name
                     sprintf "%sBuilder" name |> FsName
                   Operations = ops |}
                |> GenBuilder
                |> Set.add
            else id
        t.Members
        |> List.choose
            (fun mber ->
                match mber.Type with
                | Constructor plist
                | InstanceMethod { Params = plist }
                | StaticMethod { Params = plist } when List.exists isRef plist ->
                    None
                | _ -> Some mber)
        |> List.fold
            (fun bindings mber ->
                match binding t mber with
                | Some gen when Set.contains gen bindings |> not ->
                    Set.add gen bindings
                | _ -> bindings)
            Set.empty
        |> expr
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
      IgnoredWarnings = [ 44u; 57u; 64u; ]
      Namespaces =
        assms
        |> Seq.collect (fun assm -> assm.Types)
        |> Seq.fold
            addType
            Map.empty }
