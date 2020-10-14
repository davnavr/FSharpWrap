[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Generation.Generate

open FSharpWrap.Tool.Reflection

let fromMembers mname (members: seq<TypeRef * Member>) =
    [
        attr
            "global.Microsoft.FSharp.Core.CompilationRepresentation"
            "global.Microsoft.FSharp.Core.CompilationRepresentationFlags.ModuleSuffix"
        sprintf "module ``%s`` =" mname
        yield!
            members
            |> Seq.mapFold
                (fun map (parent, mber) ->
                    let name = Member.fsname mber
                    match Map.tryFind name map with
                    | Some _ ->
                        [ sprintf "// Duplicate generated member %s" name ]
                    | None ->
                        let gen mparams body =
                            [
                                ParamList.print mparams |> sprintf "let inline ``%s`` %s =" name
                                yield! block body |> indented
                            ]
                        match mber with
                        | InstanceMethod mthd ->
                            let plist =
                                mthd.Params
                                |> ParamList.ofList
                                |> ParamList.append
                                    { ArgType = TypeArg parent
                                      ParamName = SimpleName "this" }
                            let self, rest =
                                let rec inner rest =
                                    function
                                    | [] -> invalidOp "Parameter list was unexpectedly empty"
                                    | [ self ] -> self, List.rev rest
                                    | h :: tail -> inner (h :: rest) tail
                                plist
                                |> ParamList.toList
                                |> inner []
                            [
                                rest
                                |> List.map (fun { ParamName = name } -> SimpleName.fsname name)
                                |> String.concat ", "
                                |> sprintf
                                    "%s.``%s``(%s)"
                                    (SimpleName.fsname self.ParamName)
                                    mthd.MethodName
                            ]
                            |> gen plist
                        | UnknownMember name' ->
                            [ sprintf "// Unknown member %s in %s" name parent.FullName ]
                        | _ -> [ "// TODO: Generate other types of members" ]
                    , map)
                Map.empty
            |> fst
            |> Seq.collect id
            |> block
            |> indented
        //yield! block
        //    [
        //        for (parent: TypeRef, mdef) in members do // TODO: How to handle method overloads?
        //            // TODO: Add extra indentation when printing out members.

        //            //let f =
        //            //    Member.fsname mdef |> sprintf "let inline ``%s`` %s"
        //            //match mdef with
        //            //| InstanceMember inst ->
        //            //    match inst with
        //            //    | InstanceProperty iprop when iprop.Setter ->
        //            //        sprintf
        //            //            "(this: %s) = this.``%s``"
        //            //            (TypeRef.fsname parent)
        //            //            iprop.Name
        //            //        |> f
        //            //| UnknownMember name ->
        //            sprintf
        //                "// Unkown member %s in type %s"
        //                (Member.fsname mdef)
        //                parent.FullName
        //    ]
        //    |> indented
    ]

let fromNamespace (name: Namespace) types =
    [
        sprintf "namespace %O" name
        yield!
            types
            |> Set.fold
                (fun map (tdef: TypeDef) ->
                    let tset =
                        Map.tryFind tdef.Name map
                        |> Option.defaultValue Set.empty
                        |> Set.add tdef
                    Map.add tdef.Name tset map)
                Map.empty
            |> Map.toSeq
            |> Seq.collect (fun (mname, tdefs) ->
                Seq.collect
                    (fun { Info = info; Members = members } ->
                        Seq.map
                            (fun mdef -> info, mdef)
                            members)
                    tdefs
                |> fromMembers (string mname))
            |> indented
    ]

let fromAssemblies (assms: AssemblyInfo list) =
    let types, dups, dupcnt =
        assms
        |> Seq.collect (fun assm -> assm.Types)
        |> Seq.fold
            (fun (types', dups', dupcnt') tdef ->
                let tset =
                    Map.tryFind tdef.Namespace types'
                    |> Option.defaultValue Set.empty
                if Set.contains tdef tset
                then types', tdef :: dups', dupcnt' + 1
                else Map.add tdef.Namespace (Set.add tdef tset) types', dups', dupcnt')
            (Map.empty, [], 0)
    [
        "// This code was automatically generated by FSharpWrap"
        "// Changes made to this file will be lost when it is regenerated"
        "// Generated code for assemblies"
        for assm in assms do
            sprintf "// - %s" assm.FullName
        sprintf "// Found %i duplicate types" dupcnt
        for dup in dups do
            sprintf "// - %s" dup.FullName
        yield!
            types
            |> Map.toSeq
            |> Seq.collect (fun (ns, tdefs) -> fromNamespace ns tdefs)
    ]
