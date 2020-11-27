#r "../_lib/Fornax.Core.dll"
#r "../_lib/Markdig.dll"

open System
open System.IO
open Markdig

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
                |> Markdown.ToHtml
              Index = Array.item 0 content |> Int32.Parse
              File = file
              Link =
                file.FullName
                |> Path.GetFileNameWithoutExtension 
                |> sprintf "/%s.html"
              Sections =
                Seq.choose
                    (fun (line: string) ->
                        if line.StartsWith "##"
                        then htrim line |> Some
                        else None)
                    content'
                |> List.ofSeq
              Title = htrim title }
    ctx
