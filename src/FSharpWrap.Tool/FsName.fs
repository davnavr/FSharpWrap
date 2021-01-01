namespace FSharpWrap.Tool

open System.Reflection
open System.Runtime.CompilerServices

/// An F# identifier.
[<IsReadOnly; Struct; StructuralComparison; StructuralEquality>]
type FsName =
    internal
    | FsName of string

    override this.ToString() = let (FsName name) = this in name

[<RequireQualifiedAccess>]
module FsName =
    let ofStr =
        function
        | null | "" -> None
        | str -> FsName str |> Some

    let ofType (t: System.Type) =
        match t.DeclaringType, MemberInfo.compiledName t with
        | _, ""
        | _, null -> invalidArg "t" "The name of the type was null or empty"
        | null, name when t.IsGenericType ->
            name.LastIndexOf '`' |> name.Remove
        | _, name -> name
        |> FsName

    let ofParameter (param: ParameterInfo) =
        ofStr param.Name |> Option.defaultValue (sprintf "_arg%i" param.Position |> FsName)

    let append (FsName name) (str: string) = FsName(name + str)
    let concat (FsName name1) (FsName name2) = FsName(name1 + name2)
