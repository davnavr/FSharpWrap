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

/// Writes a newline.
let nl (printer: Printer) = printer.WriteLine String.Empty

let fsname (FsName name) (printer: Printer) =
    printer.Write "``"
    printer.Write name
    printer.Write "``"

/// Writes the name of a namespace.
let ns (Namespace names) (printer: Printer) =
    match names with
    | [] -> printer.Write "global"
    | head :: tail ->
        fsname head printer
        for name in tail do
            printer.Write "."
            fsname name printer

type PrintExpr = Printer -> unit

type PrintBuilder internal() =
    member inline _.Combine(one: PrintExpr, two: PrintExpr) =
        fun printer -> one printer; two printer
    member inline _.Delay(f: unit -> PrintExpr) = fun printer -> f () printer
    member inline _.For(items: seq<'T>, body: 'T -> PrintExpr) =
        fun printer -> for item in items do body item printer
    member inline _.Yield(str: string): PrintExpr = fun printer -> printer.WriteLine str
    member inline _.Yield(f: PrintExpr) = f
    member inline _.Zero() = ignore<Printer>

let print = PrintBuilder()
