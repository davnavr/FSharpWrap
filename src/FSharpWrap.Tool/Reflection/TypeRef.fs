[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeRef

let fsname (t: TypeRef) =
    sprintf
        "%s.%s"
        (Namespace.identifier t.Namespace)
        t.Name
        // TODO: include generic arguments/parameters.

let ofType (t: System.Type) =
    { Name = t.Name // TODO: How to handle names of generic types or nested types inside of generic types?
      Namespace = Namespace.ofStr t.Namespace
      TypeArgs = [] (*TODO: Add type arguments*) }
