#r "_lib/Fornax.Core.dll"

open Config
open System.IO

let config = {
    Generators = [
        { Script = "index.fsx"; Trigger = Once; OutputFile = NewFileName "index.html" }
        { Script = "page.fsx"; Trigger = OnFileExt ".md"; OutputFile = ChangeExtension ".html" }
    ]
}
