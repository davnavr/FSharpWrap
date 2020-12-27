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

    // TODO: Mark these methods below as Obsolete.
    member this.WriteNamespace(names: Namespace) = ns names this 

    //[<Obsolete>]
    member this.WriteComment(str: string) = comment str this

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

let comment str (printer: Printer) =
    printer.Write "// "
    printer.WriteLine str
