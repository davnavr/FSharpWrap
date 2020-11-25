#r "../_lib/Fornax.Core.dll"
#load "./layout.fsx"

open Html

let generate (content: SiteContents) (root: string) (_: string) =
    [
        h1 [] [ !!"FSharpWrap" ]
        code [] [ !!"dotnet add package FSharpWrap" ]
    ]
    |> Layout.write
        content
        "FSharpWrap - Generated F# utility functions for your dependencies"
