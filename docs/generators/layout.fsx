#r "../_lib/Fornax.Core.dll"
#if !FORNAX
#load "../loaders/article.fsx"
#endif

open Html

let write (ctx: SiteContents) title content =
    html [] [
        head [] [
            meta [ CharSet "utf-8" ]
            meta [ Name "viewport"; Content "width=device-width, initial-scale=1" ]
            Html.title [] [ !!(sprintf "FSharpWrap - %s" title) ]
            link [ Rel "stylesheet"; Href "style/main.css" ]
            link [ Rel "stylesheet"; Href "//cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/styles/vs2015.min.css" ]
            script [ Src "//cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/highlight.min.js" ] []
            script [ Src "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/languages/fsharp.min.js" ] []
            script [ Src "js/codecopy.js" ] []
            script [] [ !!"hljs.initHighlightingOnLoad();" ]
        ]

        body [] [
            nav [ Class "navbar" ] [
                let articles =
                    ctx.GetValues<Article.Info>()
                    |> List.ofSeq
                    |> List.sortBy (fun { Index = i } -> i)
                let links map =
                    List.map
                        (fun item ->
                            let title, url, liprops = map item
                            li liprops [
                                a [ Class "navbar__link"; Href url ] [ !!title ]
                            ])
                    >> ul [ Class "navbar__urls" ]

                h1 [] [ !!"FSharpWrap" ]
                links
                    (fun (article: Article.Info) ->
                        let liprops =
                            if title.EndsWith article.Title
                            then [ Class "navbar__selected" ]
                            else []
                        article.Title, article.Link, liprops)
                    articles
                h2 [ Class "navbar__title" ] [ !!"Links" ]
                links
                    (fun (name, url) -> name, url, [])
                    [
                        "GitHub", "https://github.com/davnavr/FSharpWrap"
                        "NuGet", "https://www.nuget.org/packages/FSharpWrap/"
                    ]
            ]

            main [ Class "article" ] content
        ]
    ]
    |> HtmlElement.ToString
