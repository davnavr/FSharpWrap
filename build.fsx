#if FAKE_DEPENDENCIES
#r "paket:
nuget Fake.Core.ReleaseNotes
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

let changelog = rootDir </> "CHANGELOG.md" |> Changelog.load

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
                                "Version", changelog.LatestEntry.NuGetVersion
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

Target.create "Pack" <| fun _ ->
    NuGetCli.NuGetPackDirectly
        (fun nparams ->
            { nparams with
                OutputPath = outDir
                Properties =
                    [
                        "Name", "FSharpWrap"
                        "PackageVersion", changelog.LatestEntry.NuGetVersion
                        "PackageReleaseNotes", sprintf "https://github.com/davnavr/FSharpWrap/blob/v%O/CHANGELOG.md" changelog.LatestEntry.SemVer
                        "RootDir", rootDir
                    ]
                Version = changelog.LatestEntry.NuGetVersion
                WorkingDir = rootDir })
        (srcDir </> "FSharpWrap" </> "FSharpWrap.nuspec")

Target.create "Publish GitHub" ignore

Target.create "Publish NuGet" ignore

Target.create "Publish" ignore

"Clean"
==> "Build Tool"
==> "Test Tool"
==> "Build Examples"
==> "Pack"
==> "Publish GitHub"
==> "Publish NuGet"
==> "Publish"

Target.runOrDefault "Pack"
