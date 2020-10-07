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
let localFeed = rootDir </> "local"
let slnFile = rootDir </> "FSharpWrap.sln"

let version = Environment.environVarOrDefault "PACKAGE_VERSION" "0.0.0"
let notes = Environment.environVar "PACKAGE_RELEASE_NOTES"

let handleErr msg: ProcessResult -> _ =
    function
    | { ExitCode = ecode } when ecode <> 0 ->
        failwithf "Process exited with code %i: %s" ecode msg
    | _ -> ()

Target.create "Clean" (fun _ ->
    Shell.cleanDir outDir
    Shell.cleanDir localFeed
    
    slnFile
    |> DotNetCli.exec id "clean"
    |> handleErr "Unexpected error while cleaning solution"
)

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

Target.create "Build Tool" (fun _ ->
    buildProj slnFile

    DotNetCli.publish
        (fun options ->
            { options with
                Configuration = DotNetCli.Release
                NoBuild = true
                NoRestore = true
                OutputPath = srcDir </> "FSharpWrap" </> "tool" |> Some })
        (srcDir </> "FSharpWrap.Tool" </> "FSharpWrap.Tool.fsproj")
)

// TODO: Copy output of build into the folder containing the .nuspec

let pushpkg todir ver _ =
    NuGetCli.NuGetPackDirectly
        (fun nparams ->
            { nparams with
                OutputPath = todir
                Properties =
                    [
                        "PackageVersion", ver
                        "PackageReleaseNotes", notes
                        "RootDir", rootDir
                    ]
                Version = ver
                WorkingDir = rootDir })
        (srcDir </> "FSharpWrap" </> "FSharpWrap.nuspec")

Target.create "Pack" (pushpkg outDir version)

Target.create "Push Local" (pushpkg localFeed "0.0.0+local")

Target.create "Build Examples" (fun _ ->
    rootDir </> "FSharpWrap.Examples.sln" |> buildProj
)

Target.create "All" ignore

"Clean"
==> "Build Tool"
==> "Pack"
==> "Push Local"
==> "Build Examples"
==> "All"

Target.runOrDefault "All"
