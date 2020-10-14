[<AutoOpen>]
module private FSharpWrap.Tool.Generation.Helpers

let indented lines = Seq.map (sprintf "    %s") lines
let block lines =
    seq {
        yield "begin"
        yield! indented lines
        yield "end"
    }

let attr name args =
    sprintf "[<%s(%s)>]" name args
