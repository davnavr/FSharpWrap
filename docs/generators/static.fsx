#r "../_lib/Fornax.Core.dll"
#load "./layout.fsx"

open Html
open System.IO

let generate (content: SiteContents) (root: string) (page: string) =
    Path.Combine(root, page) |> File.ReadAllBytes
