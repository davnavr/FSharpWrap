#r "_lib/Fornax.Core.dll"
#r "../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Markdown.dll"

open Config
open System.IO

// TODO: Reference any libraries using Paket load scripts.
let config = {
    Generators = [
        let page (file: string) =
            let name = Path.GetFileNameWithoutExtension file |> sprintf "%s.html"
            let dir = Path.GetDirectoryName file |> Path.GetDirectoryName
            Path.Combine(dir, name)

        let stat (_: string, page: string) =
            (page.StartsWith "style" && page.EndsWith ".css") || (page.StartsWith "js" && page.EndsWith ".js")

        let literate (_: string, page: string) =
            page.EndsWith ".fsx" && page.StartsWith "content"

        { Script = "page.fsx"; Trigger = OnFileExt ".md"; OutputFile = Custom page }
        { Script = "page.fsx"; Trigger = OnFilePredicate literate; OutputFile = Custom page }
        { Script = "static.fsx"; Trigger = OnFilePredicate stat; OutputFile = SameFileName }
    ]
}
