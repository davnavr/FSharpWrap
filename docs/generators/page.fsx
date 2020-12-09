#r "../_lib/Fornax.Core.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Common.dll"
#if !FORNAX
#load "../loaders/article.fsx"
#endif

open System.IO

open Html

let generate (content: SiteContents) (root: string) (page: string) =
    let articles =
        content.TryGetValues<Article.Info>() |> Option.defaultValue Seq.empty
    let page' =
        let path = Path.GetFullPath page
        Seq.find
            (fun (article: Article.Info) -> article.File.FullName = path)
            articles
    html [] [
        head [] [
            meta [ CharSet "utf-8" ]
            meta [ Name "viewport"; Content "width=device-width, initial-scale=1" ]
            Html.title [] [ !!(sprintf "FSharpWrap - %s" page'.Title) ]
            link [ Rel "stylesheet"; Href "./style/main.css" ]
            script [ Src "./js/codecopy.js" ] []
        ]

        body [] [
            nav [ Class "navbar infobar" ] [
                let link url title =
                    a [ Class "navbar__link"; Href url ] [ !!title ]

                h1 [] [ !!"FSharpWrap" ]

                ul [ Class "navbar__urls" ] [
                    let articles' = Seq.sortBy (fun { Article.Info.Index = i } -> i) articles

                    for article in articles' do
                        let url = "." + article.Link
                        let liclass =
                            if article = page'
                            then [ Class "navbar__selected" ]
                            else []
                        li liclass [ link url article.Title ]
                ]

                h2 [ Class "navbar__title" ] [ !!"Links" ]

                ul [ Class "navbar__urls" ] [
                    li [] [ link "https://github.com/davnavr/FSharpWrap" "GitHub" ]
                    li [] [ link "https://www.nuget.org/packages/FSharpWrap/" "NuGet" ]
                ]
            ]

            main [ Class "article" ] [
                !!page'.Content
            ]

            aside [ Class "tocbar infobar" ] [
                h3 [] [ !!"Contents" ]

                ul [ Class "tocbar__contents" ] [
                    for section in page'.Sections do
                        let url =
                            section.Replace(' ', '-') |> sprintf "#%s"
                        li [] [
                            a [ Class "tocbar__link"; Href url ] [ !!section ]
                        ]
                ]
            ]
        ]
    ]
    |> HtmlElement.ToString
