namespace FSharpWrap.Tool.Reflection

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TypeArgList =
    [<CustomComparison; CustomEquality>]
    type TypeArgList<'TypeArg> =
        private
        | TypeArg of 'TypeArg
        | TypeArgPair of 'TypeArg * 'TypeArg
        | TypeArgs of 'TypeArg[]

        member this.Length =
            match this with
            | TypeArg _ -> 1
            | TypeArgPair _ -> 2
            | TypeArgs items -> items.Length

        override this.Equals obj =
            this.Length = (obj :?> TypeArgList<'TypeArg>).Length

        override this.GetHashCode() = this.Length

        interface System.IComparable with
            member this.CompareTo obj = this.Length - (obj :?> TypeArgList<'TypeArg>).Length

    let empty = TypeArgs Array.empty
    let length (targs: TypeArgList<_>) = targs.Length
    let ofArray =
        function
        | [| targ |] -> TypeArg targ
        | [| h; t |] -> TypeArgPair(h, t)
        | targs -> TypeArgs targs
    let ofSeq targs = Seq.toArray targs |> ofArray
    let toList =
        function
        | TypeArg targ -> [ targ ]
        | TypeArgPair(h, t) -> [ h; t ]
        | TypeArgs targs -> List.ofArray targs

type TypeArgList<'TypeArg> = TypeArgList.TypeArgList<'TypeArg>

[<AutoOpen>]
module TypeArgListPatterns =
    let (|TypeArgs|) = TypeArgList.toList
