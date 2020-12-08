#r "../_lib/Fornax.Core.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Markdown.dll"
#load "./layout.fsx"

open Html
open System.IO

let generate (content: SiteContents) (root: string) (page: string) =
    Path.Combine(root, page) |> File.ReadAllBytes
