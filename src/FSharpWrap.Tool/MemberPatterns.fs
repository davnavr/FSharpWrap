[<AutoOpen>]
module FSharpWrap.Tool.MemberPatterns

open System
open System.Reflection

let (|Constructor|Event|Field|Method|Property|Type|) (mber: MemberInfo) =
    match mber with
    | :? ConstructorInfo as c -> Choice1Of6 c
    | :? EventInfo as e -> Choice2Of6 e
    | :? FieldInfo as f -> Choice3Of6 f
    | :? MethodInfo as m -> Choice4Of6 m
    | :? PropertyInfo as p -> Choice5Of6 p
    | :? Type as t -> Choice6Of6 t
    | _ ->
        mber.GetType()
        |> sprintf "Unknown member type %O"
        |> invalidArg "mber"

let inline (|Static|Instance|) (mber: ^T) =
    if (^T : (member IsStatic: bool) mber) then Choice1Of2 mber else Choice2Of2 mber

let (|IsIndexer|_|) (prop: PropertyInfo) =
    if prop.GetIndexParameters() |> Array.isEmpty |> not
    then Some prop
    else None

let (|IsPropAccessor|_|): MemberInfo -> _ =
    let check (mthd: MethodInfo) =
        mthd.DeclaringType.GetProperties()
        |> Seq.collect (fun prop -> prop.GetAccessors())
        |> Seq.contains mthd
    function
    | :? MethodInfo as mthd when check mthd -> Some()
    | _ -> None

let (|GenericParam|GenericArg|) (t: Type) =
    if t.IsGenericParameter
    then t.GetGenericParameterConstraints() |> Choice1Of2
    else Choice2Of2 t

let (|GenericArgs|) (t: Type) = t.GetGenericArguments()

let (|IsArray|_|) (t: Type) =
    if t.IsArray then t.GetElementType() |> Some else None

let (|IsByRef|_|) (t: Type) =
    if t.IsByRef then t.GetElementType() |> Some else None

let (|IsPointer|_|) (t: Type) =
    if t.IsPointer then t.GetElementType() |> Some else None

let (|IsReadOnly|_|) (prop: PropertyInfo) =
    if prop.CanRead && not prop.CanWrite
    then Some prop
    else None

let (|NamedType|_|) ns name (t: Type) =
    if t.Namespace = ns && t.Name = name
    then Some t
    else None

let (|NestedType|_|) (t: Type) =
    if t.IsNested then Some t else None

let rec (|DerivesType|_|) ns name =
    function
    | NamedType ns name t -> Some t
    | t ->
        t.BaseType
        |> Option.ofObj
        |> Option.bind ((|DerivesType|_|) ns name)

let (|TupleType|_|) =
    let tuples =
        List.allPairs
            [
                "Tuple"
                "ValueTuple"
            ]
            [ 1..8 ]
        |> List.map
            (fun (name, count) -> sprintf "%s`%i" name count)
        |> Set.ofList
    fun (t: Type) ->
        if t.Namespace = "System" && Set.contains t.Name tuples
        then Some t
        else None

let (|SpecialMethod|_|) (mthd: MethodInfo) =
    if mthd.IsSpecialName then Some mthd else None

let (|StaticProp|InstanceProp|) (prop: PropertyInfo) =
    let mthd =
        if prop.CanRead
        then prop.GetMethod
        else prop.SetMethod
    if mthd.IsStatic
    then Choice1Of2 prop
    else Choice2Of2 prop
