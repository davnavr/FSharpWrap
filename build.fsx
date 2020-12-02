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
let docsDir = rootDir </> "docs"
let outDir = rootDir </> "out"
let srcDir = rootDir </> "src"
let slnFile = rootDir </> "FSharpWrap.sln"
let testDir = rootDir </> "test"

let version = Environment.environVarOrDefault "PACKAGE_VERSION" "0.0.0"

[<AutoOpen>]
module Helpers =
    let handleErr msg: ProcessResult -> _ =
        function
        | { ExitCode = ecode } when ecode <> 0 ->
            failwithf "Process exited with code %i: %s" ecode msg
        | _ -> ()

    let buildProj proj props =
        DotNetCli.build
            (fun opt ->
                { opt with
                    Configuration = DotNetCli.Release
                    MSBuildParams =
                        { opt.MSBuildParams with
                            Properties = props }
                    NoRestore = true })
            proj

    let runProj args proj =
        [
            "--configuration Release"
            "--no-build"
            "--no-restore"
        ]
        |> args
        |> String.concat " "
        |> sprintf
            "--project %s %s"
            proj
        |> DotNetCli.exec
            id
            "run"

Target.create "Clean" <| fun _ ->
    Shell.cleanDir outDir
    Shell.cleanDir (docsDir </> "_public")

    !!(testDir </> "**" </> "*.autogen.fs") |> File.deleteAll

    // Clean all solutions with all configurations
    List.collect
        (fun cfg ->
            [
                cfg, slnFile
                cfg, rootDir </> "FSharpWrap.TestProjects.sln"
            ])
        [ "Debug"; "Release" ]
    |> List.iter
        (fun (cfg, sln) ->
            let err =
                sprintf "Unexpected error while cleaning solution %s" sln
            [
                sln
                sprintf "--configuration %s" cfg
                "--nologo"
            ]
            |> String.concat " "
            |> DotNetCli.exec id "clean"
            |> handleErr err)

Target.create "Build Tool" <| fun _ ->
    buildProj slnFile [ "Version", version ]

    DotNetCli.publish
        (fun options ->
            { options with
                Configuration = DotNetCli.Release
                NoBuild = true
                NoRestore = true
                OutputPath = srcDir </> "FSharpWrap" </> "tool" |> Some })
        (srcDir </> "FSharpWrap.Tool" </> "FSharpWrap.Tool.fsproj")

Target.create "Test Tool" <| fun _ ->
    testDir </> "FSharpWrap.Tool.Tests" </> "FSharpWrap.Tool.Tests.fsproj"
    |> runProj id
    |> handleErr "One or more tests failed"

Target.create "Test MSBuild" <| fun _ ->
    let path = rootDir </> "FSharpWrap.TestProjects.sln"
    DotNetCli.restore id path
    buildProj
        path
        [
            "_FSharpWrapLaunchDebugger", Environment.environVarOrDefault "DEBUG_FSHARPWRAP_TOOL" "false"
        ]

Target.create "Run Benchmarks" <| fun _ ->
    rootDir </> "benchmarks" </> "FSharpWrap.Tool.Benchmarks.fsproj"
    |> runProj
        (fun _ ->
            [
                "--configuration Release"
                "--framework net5.0"
                "--no-restore"
                "--"
                "--runtimes netcoreapp31 netcoreapp50"
                "--filter *"
                "--artifacts"
                outDir </> "BenchmarkDotNet.Artifacts"
            ])
    |> handleErr "One or more benchmarks could not be run successfully"

Target.create "Build Documentation" <| fun _ ->
    Shell.chdir docsDir
    DotNetCli.exec id "fornax" "build" |> handleErr "An error occured while building documentation"
    Shell.chdir rootDir

Target.create "Pack" <| fun _ ->
    let nuspec = srcDir </> "FSharpWrap" </> "FSharpWrap.nuspec"
    NuGetCli.NuGetPackDirectly
        (fun nparams ->
            { nparams with
                OutputPath = outDir
                Properties =
                    [
                        "Name", "FSharpWrap"
                        "PackageVersion", version
                        "PackageReleaseNotes", sprintf "https://github.com/davnavr/FSharpWrap/blob/v%s/CHANGELOG.md" version
                        "RootDir", rootDir
                    ]
                Version = version
                WorkingDir = rootDir })
        nuspec

"Clean"
==> "Build Tool"
==> "Test Tool"
==> "Test MSBuild"
==> "Pack"

"Test Tool" ==> "Run Benchmarks" ?=> "Test MSBuild"
"Run Benchmarks" ==> "Pack"

"Clean" ==> "Build Documentation" ?=> "Run Benchmarks"
"Build Documentation" ==> "Pack"

Target.runOrDefault "Test MSBuild"
