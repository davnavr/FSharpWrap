namespace rec FSharpWrap.Tool

open System

[<StructuralComparison; StructuralEquality>]
type TypeParam =
    { //Constraints: GenericConstraints // TODO: Add constraints for type parameters.
      ParamName: FsName }

[<StructuralComparison; StructuralEquality>]
type TypeName = // TODO: Should this be a struct?
    { Name: FsName
      Namespace: Namespace
      Parent: TypeName option
      TypeArgs: TypeArg[] }

[<StructuralComparison; StructuralEquality>]
type TypeArg =
    | ArrayType of
           {| ElementType: TypeArg
              Rank: uint |}
    | ByRefType of TypeArg
    | FsFuncType of TypeArg * TypeArg
    | InferredType
    | PointerType of TypeArg
    | TypeName of TypeName
    | TypeParam of TypeParam

[<RequireQualifiedAccess>]
module Type =
    let nameComparer =
        { new System.Collections.Generic.IEqualityComparer<TypeName> with
            member _.Equals(x, y) =
                x.Name = y.Name && x.Namespace = y.Namespace && x.Parent = y.Parent
            member _.GetHashCode obj =
                hash (obj.Name, obj.Namespace, obj.Parent) }

    let name (GenericArgs gargs as t) =
        let parent = Option.ofObj t.DeclaringType
        { Name = FsName.ofType t
          Namespace = Namespace.ofStr t.Namespace
          Parent = Option.map name parent
          TypeArgs =
            match parent with
            | None -> gargs
            | Some (GenericArgs inherited) ->
                Array.where
                    (fun garg -> Array.contains garg inherited |> not)
                    gargs
            |> Array.map arg }

    // TODO: Consider having a cache that stores a Dictionary<Type, TypeArg>.
    let arg (t: Type) =
        match t with
        | GenericParam constraints ->
            TypeParam { ParamName = FsName.ofType t }
        | IsArray elem ->
            {| ElementType = arg elem
               Rank = t.GetArrayRank() |> uint |}
            |> ArrayType
        | IsByRef tref -> arg tref |> ByRefType
        | IsPointer pnt -> arg pnt |> PointerType
        | _ -> name t |> TypeName
