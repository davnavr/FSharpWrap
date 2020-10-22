namespace FSharpWrap.Tool.Reflection

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TypeArgList =
    [<CustomComparison; CustomEquality>]
    type TypeArgList<'TypeArg> =
        private
        | TypeArgs of 'TypeArg list * int

        member this.Length =
            let (TypeArgs (_, len)) = this in len

        override this.Equals obj =
            this.Length = (obj :?> TypeArgList<'TypeArg>).Length

        override this.GetHashCode() = this.Length

        interface System.IComparable with
            member this.CompareTo obj =
                this.Length - (obj :?> TypeArgList<'TypeArg>).Length

    let length (targs: TypeArgList<_>) = targs.Length
    let toList (TypeArgs (items, _)) = items

    let ofList targs = TypeArgs(targs, List.length targs)

type TypeArgList<'TypeArg> = TypeArgList.TypeArgList<'TypeArg>

[<AutoOpen>]
module TypeArgListPatterns =
    let (|TypeArgs|) = TypeArgList.toList
