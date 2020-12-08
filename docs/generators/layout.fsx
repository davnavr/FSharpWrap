#r "../_lib/Fornax.Core.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Markdown.dll"
#if !FORNAX
#load "../loaders/article.fsx"
#endif

open Html

let write (ctx: SiteContents) title content sections =
    html [] [
        head [] [
            meta [ CharSet "utf-8" ]
            meta [ Name "viewport"; Content "width=device-width, initial-scale=1" ]
            Html.title [] [ !!(sprintf "FSharpWrap - %s" title) ]
            link [ Rel "stylesheet"; Href "./style/main.css" ]
            link [ Rel "stylesheet"; Href "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/styles/vs2015.min.css" ]
            script [ Src "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/highlight.min.js" ] []
            script [ Src "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/languages/fsharp.min.js" ] []
            script [] [ !!"hljs.initHighlightingOnLoad();" ]
            script [ Src "./js/codecopy.js" ] []
            script [ Src "./js/sectionlink.js" ] []
        ]

        let links ulclass aclass map =
            List.map
                (fun item ->
                    let title, url, liprops = map item
                    li liprops [
                        let aclass' = sprintf "infobar__link %s" aclass
                        a [ Class aclass'; Href url ] [ !!title ]
                    ])
            >> ul [ Class ulclass ]

        body [] [
            nav [ Class "navbar infobar" ] [
                let articles =
                    ctx.GetValues<Article.Info>()
                    |> List.ofSeq
                    |> List.sortBy (fun { Index = i } -> i)
                let links' map = links "navbar__urls" "navbar__link" map

                h1 [] [ !!"FSharpWrap" ]
                links'
                    (fun (article: Article.Info) ->
                        let liprops =
                            if title.EndsWith article.Title
                            then [ Class "navbar__selected" ]
                            else []
                        article.Title, "." + article.Link, liprops)
                    articles
                h2 [ Class "navbar__title" ] [ !!"Links" ]
                links'
                    (fun (name, url) -> name, url, [])
                    [
                        "GitHub", "https://github.com/davnavr/FSharpWrap"
                        "NuGet", "https://www.nuget.org/packages/FSharpWrap/"
                    ]
            ]

            main [ Class "article" ] content

            aside [ Class "tocbar infobar" ] [
                h3 [] [ !!"Contents" ]
                links
                    "tocbar__contents"
                    "tocbar__link"
                    (fun (name, url) -> name, url, [])
                    sections
            ]
        ]
    ]
    |> HtmlElement.ToString
