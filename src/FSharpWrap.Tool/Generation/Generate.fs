[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Generation.Generate

open FSharpWrap.Tool.Reflection

let fromMembers mname (members: seq<TypeName * Member>) =
    [
        attr
            "global.Microsoft.FSharp.Core.CompilationRepresentation"
            "global.Microsoft.FSharp.Core.CompilationRepresentationFlags.ModuleSuffix"
        Print.fsname mname |> sprintf "module %s ="
        yield!
            members
            |> Seq.mapFold
                (fun map (parent, mber) ->
                    let name = Print.memberName mber
                    match Map.tryFind name map with
                    | Some _ ->
                        [ sprintf "// Duplicate generated member %s" name ]
                    | None ->
                        let gen mparams body =
                            [
                                ParamList.print mparams |> sprintf "let inline ``%s`` %s =" name
                                yield! block body |> indented
                            ]
                        let self =
                            { ArgType = TypeName parent |> TypeArg
                              ParamName = FsName "this" }
                        match mber with
                        | Constructor cparams ->
                            let cparams' = ParamList.ofList cparams
                            [
                                cparams'
                                |> ParamList.toList
                                // TODO: Factor out duplicate code for params
                                |> List.map (fun { ParamName = name } -> Print.fsname name)
                                |> String.concat ", "
                                |> sprintf
                                    "new %s(%s)"
                                    (Print.typeName parent)
                            ]
                            |> gen cparams'
                        | InstanceField (ReadOnlyField field) ->
                            [
                                sprintf
                                    "%s.``%s``"
                                    (Print.fsname self.ParamName)
                                    field.FieldName
                            ]
                            |> gen (ParamList.singleton self)
                        | StaticField (ReadOnlyField field) ->
                            [
                                sprintf
                                    "%s.``%s``"
                                    (Print.typeRef field.FieldType)
                                    field.FieldName
                            ]
                            |> gen ParamList.empty
                        | InstanceField field ->
                            [
                                let name = (Print.fsname self.ParamName)
                                let fname = field.FieldName
                                sprintf
                                    "(fun() -> %s.``%s``), (fun value -> %s.``%s`` <- value)"
                                    name
                                    fname
                                    name
                                    fname
                            ]
                            |> gen (ParamList.singleton self)
                        | InstanceMethod mthd ->
                            let plist =
                                mthd.Params
                                |> ParamList.ofList
                                |> ParamList.append self
                            let rest =
                                let rec inner rest =
                                    function
                                    | []
                                    | [ _ ] -> List.rev rest
                                    | h :: tail -> inner (h :: rest) tail
                                plist
                                |> ParamList.toList
                                |> inner []
                            [
                                rest
                                |> List.map (fun { ParamName = name } -> Print.fsname name)
                                |> String.concat ", "
                                |> sprintf
                                    "%s.``%s``(%s)"
                                    (Print.fsname self.ParamName)
                                    (fst mthd.MethodName)
                            ]
                            |> gen plist
                        | UnknownMember _ ->
                            TypeName.full parent
                            |> sprintf
                                "// Unknown member %s in %s"
                                name
                            |> List.singleton
                        | _ -> [ "// TODO: Generate other types of members" ]
                    , Map.add name mber map)
                Map.empty
            |> fst
            |> Seq.collect id
            |> block
            |> indented
    ]

let fromNamespace (name: Namespace) types =
    [
        Print.ns name |> sprintf "namespace %s"
        yield!
            types
            |> Set.fold
                (fun map (tdef: TypeDef) ->
                    let tset =
                        Map.tryFind tdef.TypeName map
                        |> Option.defaultValue Set.empty
                        |> Set.add tdef
                    Map.add tdef.TypeName tset map)
                Map.empty
            |> Map.toSeq
            |> Seq.collect (fun (mname, tdefs) ->
                Seq.collect
                    (fun { TypeName = tname; Members = members } ->
                        Seq.map
                            (fun mdef -> tname, mdef)
                            members)
                    tdefs
                |> fromMembers mname.Name)
            |> indented
    ]

let fromAssemblies (assms: AssemblyInfo list) =
    let types, dups, dupcnt =
        assms
        |> Seq.collect (fun assm -> assm.Types)
        |> Seq.fold
            (fun (types', dups', dupcnt') tdef ->
                let tset =
                    Map.tryFind tdef.TypeName.Namespace types'
                    |> Option.defaultValue Set.empty
                if Set.contains tdef tset
                then types', tdef :: dups', dupcnt' + 1
                else Map.add tdef.TypeName.Namespace (Set.add tdef tset) types', dups', dupcnt')
            (Map.empty, [], 0)
    [
        "// This code was automatically generated by FSharpWrap"
        "// Changes made to this file will be lost when it is regenerated"
        "// Generated code for assemblies"
        for assm in assms do
            sprintf "// - %s" assm.FullName
        sprintf "// Found %i duplicate types" dupcnt
        for dup in dups do
            dup.TypeName
            |> TypeName.full
            |> sprintf "// - %s"
        yield!
            types
            |> Map.toSeq
            |> Seq.collect (fun (ns, tdefs) -> fromNamespace ns tdefs)
    ]
