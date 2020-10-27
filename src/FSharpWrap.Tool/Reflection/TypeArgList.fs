namespace FSharpWrap.Tool.Reflection

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TypeArgList =
    [<CustomComparison; CustomEquality>]
    type TypeArgList<'TypeArg> =
        private
        | TypeArgs of 'TypeArg[]

        member this.Length =
            let (TypeArgs items) = this in items.Length

        override this.Equals obj =
            this.Length = (obj :?> TypeArgList<'TypeArg>).Length

        override this.GetHashCode() = this.Length

        interface System.IComparable with
            member this.CompareTo obj =
                this.Length - (obj :?> TypeArgList<'TypeArg>).Length

    let length (targs: TypeArgList<_>) = targs.Length
    let toList (TypeArgs targs) = List.ofArray targs

    let ofArray = TypeArgs
    let ofSeq targs = Seq.toArray targs |> ofArray

type TypeArgList<'TypeArg> = TypeArgList.TypeArgList<'TypeArg>

[<AutoOpen>]
module TypeArgListPatterns =
    let (|TypeArgs|) = TypeArgList.toList
