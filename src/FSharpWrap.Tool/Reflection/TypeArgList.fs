[<RequireQualifiedAccess>]
module  FSharpWrap.Tool.Reflection.TypeArgList

[<CustomComparison; CustomEquality>]
type TypeArgList<'TypeArg> =
    private
    | TypeArgs of 'TypeArg list * int

    member this.Length =
        let (TypeArgs (_, len)) = this in len

    override this.Equals obj =
        this.Length = (obj :?> TypeArgList<_>).Length

    override this.GetHashCode() = this.Length

    interface System.IComparable with
        member this.CompareTo obj =
            this.Length - (obj :?> TypeArgList<_>).Length

let length (targs: TypeArgList<_>) = targs.Length
let toList (TypeArgs (items, _)) = items

let ofList targs = TypeArgs(targs, List.length targs)
