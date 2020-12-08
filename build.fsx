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
let mainSln = rootDir </> "FSharpWrap.sln"
let testSln = rootDir </> "FSharpWrap.TestProjects.sln"
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
                    MSBuildParams = { opt.MSBuildParams with Properties = props }
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

Target.create "Restore" <| fun _ ->
    for args in [ ""; "--group Test" ] do
        DotNetCli.exec
            id
            "paket"
            (sprintf "restore %s" args)
        |> handleErr "Error occured while restoring packages"

Target.create "Clean" <| fun _ ->
    Shell.cleanDir outDir
    Shell.cleanDir (docsDir </> "_public")

    !!(testDir </> "**" </> "*.autogen.fs") |> File.deleteAll

    List.allPairs
        [ "Debug"; "Release" ]
        [
            mainSln
            rootDir </> "FSharpWrap.TestProjects.sln"
        ]
    |> List.iter
        (fun (cfg, sln) ->
            let err =
                sprintf "Unexpected error while cleaning solution %s" sln
            [
                sln
                sprintf "--configuration %s" cfg
            ]
            |> String.concat " "
            |> DotNetCli.exec id "clean"
            |> handleErr err)

Target.create "Build Tool" <| fun _ ->
    buildProj mainSln [ "Version", version; "TreatWarningsAsErrors", "true" ]

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

Target.create "Build MSBuild" <| fun _ ->
    buildProj
        testSln
        [
            "_FSharpWrapLaunchDebugger", Environment.environVarOrDefault "DEBUG_FSHARPWRAP_TOOL" "false"
            "TreatWarningsAsErrors", "true"
        ]

Target.create "Test MSBuild" <| fun _ ->
    let run proj tfms =
        let msg = sprintf "Error while running test project %s" proj
        List.iter
            (fun tfm ->
                runProj
                    (fun args -> sprintf "--framework %s" tfm :: args)
                    proj
                |> handleErr msg)
            tfms
    [
        "TestProject.Collections" </> "TestProject.Collections.fsproj", [ "netcoreapp3.1" ]
        "TestProject.CSharpDependent" </> "TestProject.CSharpDependent.fsproj", [ "netcoreapp3.1" ]
        "TestProject.MultiTarget" </> "TestProject.MultiTarget.fsproj", [ "netcoreapp3.1"; "net5.0" ]
    ]
    |> Map.ofList
    |> Map.iter (fun proj -> testDir </> proj |> run)

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

"Restore"
==> "Clean"
==> "Build Tool"
==> "Test Tool"
==> "Build MSBuild"
==> "Test MSBuild"
==> "Pack"

"Test Tool" ==> "Run Benchmarks" ?=> "Build MSBuild"
"Run Benchmarks" ==> "Pack"

"Clean" ==> "Build Documentation" ?=> "Run Benchmarks"
"Build Documentation" ==> "Pack"

Target.runOrDefault "Test MSBuild"
