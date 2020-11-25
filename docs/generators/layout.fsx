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
        ]

    html [] [
        head [] [
            meta [ CharSet "utf-8" ]
            meta [ Name "viewport"; Content "width=device-width, initial-scale=1" ]
            Html.title [] [ !!(sprintf "FSharpWrap - %s" title) ]
            link [ Rel "stylesheet"; Href "style/main.css" ]
        ]

        body [] [
            nav [ Class "links-bar" ] [
                h1 [ Class "links-bar__logo" ] [ !!"FSharpWrap" ]

                for (name, link) in links do
                    a [ Href link ] [ !!name ]
            ]

            main [ Class "article" ] content
        ]
    ]
    |> HtmlElement.ToString
