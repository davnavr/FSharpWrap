module rec FSharpWrap.Tool.Print // TODO: Remove "rec".

open System
open System.IO

/// Prints formatted F# source code.
type Printer(writer: StreamWriter) = // TODO: Create computation expression.
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

let accessor name tname (field: string) =
    print {
        "let "
        fsname name
        " (this: "
        fsname tname
        ") = this."
        field
    }

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
        nl
        "end"
        dedent
        nl
    }

type PrintExpr = Printer -> unit


