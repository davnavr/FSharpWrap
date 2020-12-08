#r "../_lib/Fornax.Core.dll"
#r "../../packages/documentation/Markdig/lib/netstandard2.0/Markdig.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.CodeFormat.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Common.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Literate.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Markdown.dll"

open System
open System.IO

open FSharp.Formatting.Literate
open FSharp.Formatting.Literate.Evaluation

type Info = 
    { Content: string
      File: FileInfo
      Index: int
      Link: string
      Sections: string list
      Title: string }

let private htrim (str: string) = str.Trim [| ' '; '#' |]

let loader (root: string) (ctx: SiteContents) =
    let dir = Path.Combine(root, "content") |> DirectoryInfo
    for file in dir.GetFiles("*.md", SearchOption.AllDirectories) do
        let content = File.ReadAllLines file.FullName
        let title = Array.item 1 content
        let content' = Seq.skip 2 content
        ctx.Add
            { Content =
                content'
                |> String.concat "\n"
                |> Markdig.Markdown.ToHtml
              Index = Array.item 0 content |> Int32.Parse
              File = file
              Link =
                file.FullName
                |> Path.GetFileNameWithoutExtension 
                |> sprintf "/%s.html"
              Sections =
                Seq.choose
                    (fun (line: string) ->
                        if line.StartsWith "## "
                        then htrim line |> Some
                        else None)
                    content'
                |> List.ofSeq
              Title = htrim title }

    let fsi = FsiEvaluator()
    for script in dir.GetFiles("*.fsx", SearchOption.AllDirectories) do
        let doc = Literate.ParseAndCheckScriptFile(script.FullName, fsiEvaluator = fsi)
        ctx.Add
            { Content = Literate.ToHtml doc // TODO: There is a generateAnchors parameter for Literate.ToHtml, use it.
              Index = 69
              File = script
              Link =
                script.FullName
                |> Path.GetFileNameWithoutExtension 
                |> sprintf "/%s.html"
              Sections = []
              Title = "Test" }
    ctx
