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
        ]

        body [] [
            main [] content
        ]
    ]
    |> HtmlElement.ToString
