#if FAKE_DEPENDENCIES
#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.DotNet.NuGet
nuget Fake.IO.FileSystem
//"
#endif

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

module DotNetCli = Fake.DotNet.DotNet
module NuGetCli = Fake.DotNet.NuGet.NuGet

let rootDir = __SOURCE_DIRECTORY__
let outDir = rootDir </> "out"
let srcDir = rootDir </> "src"
let slnFile = rootDir </> "FSharpWrap.sln"

let version = Environment.environVarOrDefault "PACKAGE_VERSION" "0.0.0"
let notes = Environment.environVar "PACKAGE_RELEASE_NOTES"

let handleErr msg: ProcessResult -> _ =
    function
    | { ExitCode = ecode } when ecode <> 0 ->
        failwithf "Process exited with code %i: %s" ecode msg
    | _ -> ()

Target.create "Clean" <| fun _ ->
    Shell.cleanDir outDir

    !!(rootDir </> "examples/**/*.autogen.fs") |> File.deleteAll
    
    slnFile
    |> DotNetCli.exec id "clean"
    |> handleErr "Unexpected error while cleaning solution"

let buildProj proj =
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
        proj

Target.create "Build Tool" <| fun _ ->
    buildProj slnFile

    DotNetCli.publish
        (fun options ->
            { options with
                Configuration = DotNetCli.Release
                NoBuild = true
                NoRestore = true
                OutputPath = srcDir </> "FSharpWrap" </> "tool" |> Some })
        (srcDir </> "FSharpWrap.Tool" </> "FSharpWrap.Tool.fsproj")

Target.create "Test Tool" <| fun _ ->
    sprintf
        "--project %s --configuration Release --no-build --no-restore"
        (rootDir </> "test" </> "FSharpWrap.Tool.Tests" </> "FSharpWrap.Tool.Tests.fsproj")
    |> DotNetCli.exec
        id
        "run"
    |> handleErr "One or more tests failed"

Target.create "Build Examples" <| fun _ ->
    let path = rootDir </> "FSharpWrap.Examples.sln"
    DotNetCli.restore id path
    buildProj path

let pushpkg todir ver _ =
    NuGetCli.NuGetPackDirectly
        (fun nparams ->
            { nparams with
                OutputPath = todir
                Properties =
                    [
                        "Name", "FSharpWrap"
                        "PackageVersion", ver
                        "PackageReleaseNotes", notes
                        "RootDir", rootDir
                    ]
                Version = ver
                WorkingDir = rootDir })
        (srcDir </> "FSharpWrap" </> "FSharpWrap.nuspec")

Target.create "Pack" (pushpkg outDir version)

"Clean"
==> "Build Tool"
==> "Test Tool"
==> "Build Examples"
==> "Pack"

Target.runOrDefault "Pack"
