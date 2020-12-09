#r "../_lib/Fornax.Core.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Markdown.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.CodeFormat.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Common.dll"
#r "../../packages/documentation/FSharp.Formatting/lib/netstandard2.0/FSharp.Formatting.Literate.dll"

open System
open System.IO

open FSharp.Formatting.CodeFormat
open FSharp.Formatting.Literate
open FSharp.Formatting.Literate.Evaluation
open FSharp.Formatting.Markdown

type Info = 
    { Category: string
      Content: string
      File: FileInfo
      Index: int
      Link: string
      Sections: string list
      Title: string }

let private (|IsHeader|_|) size =
    function
    | Heading(size', [ Literal(text, _) ], _) when size' = size -> Some text
    | _ -> None

let private (|IsLiterateCode|_|) (ps: MarkdownEmbedParagraphs) =
    match ps with
    | :? LiterateParagraph as p -> Some p
    | _ -> None

let private (|HasConfig|_|) =
    function
    | Line("(*", _) :: Line(config, _) :: Line("*)", _) :: _ ->
        let config' = config.Split ';'
        {| Index =
            Array.tryPick
                (fun (str: string) ->
                    if str.StartsWith "index=" then
                        str.Substring(6)
                        |> Int32.Parse
                        |> Some
                    else
                        None)
                config'
            |> Option.defaultValue Int32.MaxValue
           Category = "" |}
        |> Some
    | _ -> None

let loader (root: string) (ctx: SiteContents) =
    try
        let dir = Path.Combine(root, "content") |> DirectoryInfo
        let fsi = FsiEvaluator()
        for script in dir.GetFiles "*.fsx" do
            let doc = Literate.ParseAndCheckScriptFile(script.FullName, fsiEvaluator = fsi)
            let config =
                match List.head doc.Paragraphs with
                | EmbedParagraphs(IsLiterateCode(LiterateCode(HasConfig config, _, _)), _) ->
                    config
                | _ -> {| Index = Int32.MaxValue; Category = "" |}
            ctx.Add
                { Category = config.Category
                  Content = Literate.ToHtml(doc, generateAnchors = true)
                  File = script
                  Index = config.Index
                  Link =
                    Path.GetFileNameWithoutExtension script.FullName |> sprintf "/%s.html"
                  Sections =
                    List.choose
                      (function
                      | IsHeader 2 text -> Some text
                      | _ -> None)
                      doc.Paragraphs
                  Title =
                    List.pick
                      (function
                      | IsHeader 1 text -> Some text
                      | _ -> None)
                      doc.Paragraphs }
        ctx
    with
    | ex -> stderr.WriteLine ex; raise ex
