#r "../_lib/Fornax.Core.dll"
#if !FORNAX
#load "../loaders/article.fsx"
#endif

open Html

let write (ctx: SiteContents) title content =
    let links =
        [
            "Home", "index.html"
            "GitHub", "https://github.com/davnavr/FSharpWrap"
            "NuGet", "https://www.nuget.org/packages/FSharpWrap/"
        ]
    let articles =
        ctx.GetValues<Article.Info>() |> List.ofSeq

    html [] [
        head [] [
            meta [ CharSet "utf-8" ]
            meta [ Name "viewport"; Content "width=device-width, initial-scale=1" ]
            Html.title [] [ !!(sprintf "FSharpWrap - %s" title) ]
            link [ Rel "stylesheet"; Href "style/main.css" ]
            link [ Rel "stylesheet"; Href "//cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/styles/vs2015.min.css" ]
            script [ Src "//cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/highlight.min.js" ] []
            script [ Src "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.4.0/languages/fsharp.min.js" ] []
            script [] [ !!"hljs.initHighlightingOnLoad();" ]
        ]

        body [] [
            nav [ Class "navbar" ] [
                h1 [ Class "navbar__logo" ] [ !!"FSharpWrap" ]

                for (name, link) in links do
                    a [ Href link ] [ !!name ]

                List.map
                    (fun (article: Article.Info) ->
                        li [] [
                            a [ Href article.Link ] [ !!article.Title ]
                        ])
                    articles
                |> ul [ Class "navbar__links" ]
            ]

            main [ Class "article" ] content
        ]
    ]
    |> HtmlElement.ToString
