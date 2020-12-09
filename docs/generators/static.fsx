#r "../_lib/Fornax.Core.dll"

open Html
open System.IO

let generate (content: SiteContents) (root: string) (page: string) =
    Path.Combine(root, page) |> File.ReadAllBytes
