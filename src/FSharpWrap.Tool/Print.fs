module FSharpWrap.Tool.Print

open System
open System.IO

/// Prints formatted F# source code.
type Printer(writer: StreamWriter) =
    [<Literal>]
    let IndentLevel = 4

    let mutable indent, indented = 0, false

    member private _.WriteIndent() =
        if not indented && indent > 0 then
            String(' ', indent * IndentLevel) |> writer.Write
            indented <- true

    member _.Indent() = indent <- indent + 1
    member _.Dedent() = if indent > 0 then indent <- indent - 1

    member this.Write(str: string) =
        this.WriteIndent()
        writer.Write str

    member this.WriteLine(str: string) =
        this.WriteIndent()
        writer.WriteLine str
        indented <- false

    interface IDisposable with member _.Dispose() = writer.Dispose()

/// Writes a newline.
let inline nl (printer: Printer) = printer.WriteLine String.Empty
let inline indent (printer: Printer) = printer.Indent()
let inline dedent (printer: Printer) = printer.Dedent()

type PrintExpr = Printer -> unit

type PrintBuilder internal() =
    member inline _.Combine(one: PrintExpr, two: PrintExpr) =
        fun printer -> one printer; two printer
    member inline _.Delay(f: unit -> PrintExpr) = fun printer -> f () printer
    member inline _.For(items: seq<'T>, body: 'T -> PrintExpr) =
        fun printer -> for item in items do body item printer
    member inline _.Yield(str: string): PrintExpr = fun printer -> printer.Write str
    member inline _.Yield(f: PrintExpr) = f
    member inline _.Zero() = ignore<Printer>

let print = PrintBuilder()

let fsname (FsName name) = print { "``"; name; "``" }

/// Writes the name of a namespace.
let ns (Namespace names) =
    print {
        match names with
        | [] -> "global"
        | head :: tail ->
            fsname head
            for name in tail do
                "."
                fsname name
    }

let rec typeName { Name = name; Namespace = nspace; Parent = parent; TypeArgs = targs } =
    print {
        match parent with
        | None -> ns nspace
        | Some parent' -> typeName parent'
        "."
        fsname name
        if Array.isEmpty targs |> not then
            let max = targs.Length - 1
            "<"
            for i = 0 to max do
                Array.get targs i |> typeArg
                if i < max then
                    ","
            ">"
    }
and typeArg t: PrintExpr =
    print {
        match t with
        | ArrayType arr ->
            let rank =
                match arr.Rank with
                | 0u | 1u -> ""
                | _ -> String(',', arr.Rank - 1u |> int)
            typeArg arr.ElementType
            "["
            rank
            "]"
        | ByRefType tref ->
            typeArg tref
            " ref"
        | FsFuncType(param, ret) ->
            "("
            typeArg param
            " -> "
            typeArg ret
            ")"
        | InferredType -> "_"
        // PointerType () -> "voidptr" // TODO: Have special case for when it is a pointer to System.Void
        | PointerType pnt ->
            "nativeptr<"
            typeArg pnt
            ">"
        | TypeName tname -> typeName tname
        | TypeParam tparam ->
            "'"
            fsname tparam.ParamName
    }

let binding (name: FsName) (body: PrintExpr) =
    print {
        "let inline "
        fsname name
        " "
        body
    }

let accessor name tname (field: string) =
    print {
        "(this: "
        typeName tname
        ") = this."
        field
    }
    |> binding name

let parameters (cache: NameCache) =
    function
    | [||] -> print { "()" }
    | (parr: Params) ->
        print {
            for (name, t) in parr do
                "("
                fsname name
                ": "
                let targ = cache.GetTypeArg t
                typeArg targ
                match targ with
                | TypeParam { Constraints = constraints } ->
                    let count = constraints.Count
                    if count > 0 then
                        " when "
                        let mutable i = 0
                        for constr in constraints do
                            i <- i + 1
                            let (TypeConstraint derives) = constr
                            typeArg targ
                            " :> "
                            typeArg derives
                            if i < count then " and "
                | _ -> ()
                ") "
        }

let arguments (args: Params) =
    print {
        "("
        let max = args.Length - 1
        for i = 0 to args.Length - 1 do
            Array.get args i |> fst |> fsname
            if i < max then
                ", "
        ")"
    }

let method name fparams (callee: PrintExpr) mname (targs: TypeArg[]) args (cache: NameCache) =
    print {
        parameters cache fparams
        " = "
        callee
        "."
        FsName mname |> fsname
        match targs with
        | [||] -> ()
        | _ ->
            "<"
            let max = targs.Length - 1
            for i = 0 to targs.Length - 1 do
                Array.get targs i |> typeArg
                if i < max then ", "
            ">"
        arguments args
    }
    |> binding name

/// <summary>
/// Writes an F# module from a <see cref="System.Type"/>.
/// </summary>
let mdle (name: FsName) (t: Type) (body: PrintExpr) =
    print {
        "[<global.Microsoft.FSharp.Core.CompilationRepresentationAttribute(global.Microsoft.FSharp.Core.CompilationRepresentationFlags.ModuleSuffix)>]"; nl
        "module internal "
        fsname name
        " ="
        nl
        indent
        "begin"
        nl
        indent
        body
        dedent
        "end"
        dedent
        nl
    }
