namespace rec FSharpWrap.Tool

open System
open System.IO

[<RequireQualifiedAccess>]
type Directory =
    { Path: string }

    override this.ToString() = this.Path

[<RequireQualifiedAccess>]
type File =
    { Extension: string
      Name: string
      Parent: Directory
      Path: string }

    override this.ToString() = this.Path

type Path =
    | File of File
    | Directory of Directory

    override this.ToString() =
        match this with
        | File { Path = path }
        | Directory { Path = path } -> path

[<RequireQualifiedAccess>]
module File =
    let ofStr =
        function
        | ""
        | null -> None
        | str ->
            if Path.EndsInDirectorySeparator str
            then None
            else
                try
                    let dir =
                        str
                        |> Path.GetDirectoryName
                        |> Directory.ofStr
                        |> Option.defaultWith
                            (fun() -> sprintf "The directory for the file '%s' could not be parsed" str |> invalidOp)
                    { File.Extension = Path.GetExtension str
                      File.Name = Path.GetFileName str
                      File.Parent = dir
                      File.Path = str }
                    |> Some
                with
                | :? ArgumentException
                | :? NotSupportedException
                | :? PathTooLongException -> None

    let (|Valid|Invalid|) str =
        match ofStr str with
        | Some file -> Choice1Of2 file
        | None -> Choice2Of2()

    let fullPath (file: File) = { file with File.Path = Path.GetFullPath file.Path }

[<RequireQualifiedAccess>]
module Directory =
    let ofStr str =
        try
            let name =
                match str with
                | ""
                | null -> None
                | _ ->
                    sprintf "%s/" str
                    |> Path.GetDirectoryName
                    |> Option.ofObj
                |> Option.defaultValue String.Empty
            Some { Directory.Path = name }
        with
        | :? ArgumentException
        | :? PathTooLongException -> None

    let (|Valid|Invalid|) str =
        match ofStr str with
        | Some file -> Choice1Of2 file
        | None -> Choice2Of2()

    let fullPath (dir: Directory) = { dir with Directory.Path = Path.GetFullPath dir.Path }

[<RequireQualifiedAccess>]
module Path =
    let ofStr =
        function
        | File.Valid file -> File file |> Some
        | Directory.Valid dir -> Directory dir |> Some
        | _ -> None

    let (|Valid|Invalid|) str =
        match ofStr str with
        | Some path -> Choice1Of2 path
        | None -> Choice2Of2()

    let fullPath =
        function
        | File file -> File.fullPath file |> File
        | Directory dir -> Directory.fullPath dir |> Directory
