#r "../_lib/Fornax.Core.dll"
#load "./layout.fsx"

open Html
open System.IO

let generate (content: SiteContents) (root: string) (page: string) =
    let article =
        let page' = Path.GetFullPath page
        content.TryGetValues<Article.Info>()
        |> Option.defaultValue Seq.empty
        |> Seq.find
            (fun article -> article.File.FullName = page')
    [
        h1 [] [ !!article.Title ]
        !!"How to add content?"
    ]
    |> Layout.write
        content
        (sprintf "FSharpWrap - %s" article.Title)
