[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Generate

open System
open System.Collections.Generic
open System.IO
open System.Reflection

open FSharpWrap.Tool.Print

type TypeIdentifier =
    | SingleType of Type
    | MultipleTypes of Map<uint32, Type>

let genericArgCount (GenericArgs gargs) = Array.length gargs |> uint32

let (|IsReadOnlyBool|_|) (prop: PropertyInfo) =
    match prop, prop.PropertyType with
    | IsReadOnly _, NamedType "System" "Boolean" _ -> Some prop
    | _ -> None

let memberName (mber: MemberInfo) = // TODO: Create a system for having one or more backup names in case one already exists.
    match mber with
    | Type t -> FsName.ofType t
    | Constructor ctor ->
        let parameters =
            ctor.GetParameters() |> Array.map (fun t -> t.ParameterType)
        match parameters with
        | [| IsArray _ |] -> FsName "ofArray"
        | [| NamedType "System" "Object" _ |] -> FsName "ofObj"
        | [| NamedType "System.Collections.Generic" "IEnumerable`1" _ |] -> FsName "ofSeq"
        | [| NamedType "System.Collections.Generic" "List`1" _ |] -> FsName "ofResizeArray"
        | [| NamedType "Microsoft.FSharp.Collections" "FSharpList`1" _ |] -> FsName "ofList"
        | [| GenericArg  t |] -> FsName.concat (FsName "of") (FsName.ofType t)
        | _ -> FsName "create"
    | Property (IsReadOnlyBool _) -> FsName mber.Name
    | Event _
    | Field _
    | Method _
    | Property _ -> String.toCamelCase mber.Name |> FsName

let rec mdle mname (t: Type): PrintExpr = // TODO: How to check for name conflicts for members contained in the module?
    let tname = Type.name t
    let members = t.GetMembers()
    let bindings = HashSet<FsName> members.Length
    let members' =
        members
        |> Seq.where (fun m -> m.DeclaringType = t)
        |> Seq.choose
            (function
            | IsPropAccessor
            // TODO: Skip static methods that represent operations (ex: op_Implicit)
            | :? EventInfo -> None
            | mber -> Some mber)

    print {
        for mber in members' do
            let name = memberName mber
            if bindings.Contains name then // TODO: How to prioritize certain method overloads?
                sprintf "// Duplicate member %s with generated name %O" mber.Name name
            else
                bindings.Add name |> ignore
                match mber with
                | Constructor ctor ->
                    let parameters = Params.ofCtor ctor
                    print {
                        Print.parameters parameters
                        " = new "
                        typeName tname
                        Print.arguments parameters
                    }
                    |> binding name
                | Event e -> sprintf "// NOTE: Generation of members for event %s is not yet supported" e.Name
                | Field(Instance field) when field.IsInitOnly -> accessor name tname field.Name
                | Method(Instance mthd) ->
                    let parameters = Params.ofMethod mthd
                    let this, parameters' = Params.ofMethod mthd |> Array.splitAt 1
                    let this', _ = Array.head this
                    print {
                        Print.parameters parameters
                        " = "
                        fsname this'
                        "."
                        FsName mthd.Name |> fsname
                        Print.arguments parameters'
                    }
                    |> binding name
                | Method(Static mthd) -> // TODO: What if the static method is a constructor for an F# union case?
                    let parameters = Params.ofMethod mthd
                    print {
                        Print.parameters parameters
                        " = "
                        typeName tname
                        "."
                        FsName mthd.Name |> fsname
                    }
                    |> binding name
                // TODO: Check if property is instance property for these two checks.
                | Property (IsIndexer prop) -> sprintf "// NOTE: Generation of member for property with parameter %s is not yet supported" prop.Name
                | Property (IsReadOnlyBool _) ->
                    print {
                        "let inline (|"
                        fsname name
                        "|_|) (this: "
                        typeName tname
                        ") = if this."
                        fsname name
                        " then Some this else None"
                    }
                | Property (IsReadOnly prop) -> accessor name tname prop.Name
                | Type t' -> mdle name t'
                | Field _ -> ()
                | Property _ -> ()
            nl

        // TODO: Create computation expression.
    }
    |> Print.mdle mname t

let fromAssemblies (assemblies: seq<Assembly>) (filter: Filter) =
    print {
        let assemblies' =
            Seq.where
                (Filter.assemblyIncluded filter)
                assemblies
            |> Array.ofSeq
        "// This code was automatically generated by FSharpWrap"; nl
        "// Changes made to this file will be lost when it is regenerated"; nl
        "// # Included Assemblies:"; nl
        for assembly in assemblies' do
            sprintf "// - %s" assembly.FullName; nl
        let namespaces =
            let types =
                assemblies'
                |> Seq.collect (fun assembly -> assembly.ExportedTypes)
                |> Seq.where
                    (fun t ->
                        not t.IsNested && Filter.typeIncluded filter t) // TODO: Include nested types inside nested modules.
            let dict = Dictionary<Namespace, Dictionary<TypeName, TypeIdentifier>> assemblies'.Length
            for t in types do
                let ns = Namespace.ofStr t.Namespace // TODO: Figure out how to cache namespaces.
                let name = Type.name t
                match dict.TryGetValue ns with
                | true, previous ->
                    let entry =
                        match previous.TryGetValue name with
                        | (true, SingleType other) ->
                            Map.empty
                            |> Map.add (genericArgCount other) other
                            |> Map.add (genericArgCount t) t
                            |> MultipleTypes
                        | (true, MultipleTypes others) ->
                            Map.add (genericArgCount t) t others |> MultipleTypes
                        | (false, _) -> SingleType t
                    previous.Item <- name, entry
                | false, _ ->
                    let entry = Dictionary 1
                    entry.Item <- name, SingleType t
                    dict.Item <- ns, entry
            dict
        for KeyValue(ns, types) in namespaces do
            "namespace "
            Print.ns ns
            nl
            indent
            for KeyValue({ Name = name }, t) in types do
                match t with
                | SingleType t' -> mdle name t'
                | MultipleTypes dups ->
                    for KeyValue(i, t') in dups do
                        let name' = FsName.append (sprintf "_%i" i) name
                        mdle name' t'
            dedent
            nl
    }

let fromResolver resolver loader filter printer =
    use context = new MetadataLoadContext(resolver)
    fromAssemblies (loader context) filter printer

let fromPaths assemblies =
    let files =
        Seq.collect
            (function
            | Directory dir ->
                Directory.EnumerateFiles(dir.Path, "*.dll", SearchOption.AllDirectories)
            | File file -> Seq.singleton file.Path)
            assemblies
    fromResolver
        (PathAssemblyResolver files)
        (fun context -> Seq.map context.LoadFromAssemblyPath files)
