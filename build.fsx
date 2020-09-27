#if FAKE_DEPENDENCIES
#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
//"
#endif

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators

module DotNetCli = Fake.DotNet.DotNet

let rootDir = __SOURCE_DIRECTORY__
let outDir = rootDir </> "out"
let slnFile = rootDir </> "FSharpWrap.sln"

let version = Environment.environVarOrDefault "PACKAGE_VERSION" "0.0.0"
let notes = Environment.environVar "PACKAGE_RELEASE_NOTES"

let handleErr msg: ProcessResult -> _ =
    function
    | { ExitCode = ecode } when ecode <> 0 ->
        failwithf "Process exited with code %i: %s" ecode msg
    | _ -> ()

let runProj args proj =
    sprintf
        "--project %s --no-build --no-restore --configuration Release -- %s"
        proj
        args
    |> DotNetCli.exec id "run"

Target.create "Clean" (fun _ ->
    Shell.cleanDir outDir

    slnFile
    |> DotNetCli.exec id "clean"
    |> handleErr "Unexpected error while cleaning solution"
)

Target.create "Build" (fun _ ->
    DotNetCli.build
        (fun opt ->
            { opt with
                Configuration = DotNetCli.Release
                MSBuildParams =
                    { opt.MSBuildParams with
                        Properties =
                            [
                                "Version", version
                            ]}
                NoRestore = true })
        slnFile
)

Target.create "Test" (fun _ ->
    Trace.log "Testing..."
)

Target.create "Pack" (fun _ ->
    Trace.log "Packing..."
)

"Clean" ==> "Build" ==> "Test" ==> "Pack"

Target.runOrDefault "Test"
