namespace FSharpWrap.Tool

open System
open System.IO

/// Prints formatted F# source code.
type Printer(writer: StreamWriter) =
    let mutable indent, indented = 0, false

    member private _.WriteIndent() =
        if not indented && indent > 0 then
            String(' ', indent * 4) |> writer.Write

    member this.Write(str: string) =
        this.WriteIndent()
        writer.Write str

    member this.WriteLine(str: string) =
        this.WriteIndent()
        writer.WriteLine str

    member this.Comment(str: string) =
        this.Write "// "
        this.WriteLine str
