#r "_lib/Fornax.Core.dll"

open Config
open System.IO

let config = {
    Generators = [
        let page (file: string) =
            let name = Path.GetFileNameWithoutExtension file |> sprintf "%s.html"
            let dir = Path.GetDirectoryName file |> Path.GetDirectoryName
            Path.Combine(dir, name)

        let style (root: string, page: string) =
            page.StartsWith "style" && page.EndsWith ".css"

        { Script = "index.fsx"; Trigger = Once; OutputFile = NewFileName "index.html" }
        { Script = "page.fsx"; Trigger = OnFileExt ".md"; OutputFile = Custom page }
        { Script = "static.fsx"; Trigger = OnFilePredicate style; OutputFile = SameFileName }
    ]
}
