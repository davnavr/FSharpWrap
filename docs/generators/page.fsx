#r "../_lib/Fornax.Core.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Markdown.dll"
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
    let article' = [
        h1 [] [ !!article.Title ]
        hr []
        !!article.Content
    ]
    let sections =
        List.map
            (fun (section: string) ->
                let url =
                    section
                        .ToLower()
                        .Replace(' ', '-')
                    |> sprintf "#%s"
                section, url)
            article.Sections
    Layout.write content article.Title article' sections
