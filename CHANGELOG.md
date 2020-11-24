# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Small performance improvements
- Additional switches to specify which assemblies and namespaces to include in code generation
### Changed
- All assemblies are included in code generation by default

## [0.3.0] - 2020-11-13
### Added
- Filtering of assemblies to avoid generating code for dependencies you won't use

## [0.2.0] - 2020-11-08
### Added
- Copying of `ObsoleteAttribute` and `ExperimentalAttribute` on correponding modules and members
- Suppression of warnings FS0044, FS0057, and FS0064 in generated code
- Generation of functions for read-only instance fields and read-only instance properties
- Code generation target now only runs for F# (`.fsproj`) projects
- Now compatible with `<ProjectReference>` dependencies
### Changed
- Exclusion of members marked `ObsoleteAttribute` when `IsError` is `true` to avoid errors that cannot be suppressed
- Exclusion of structs not marked with `IsReadOnlyAttribute` from code generation to avoid problems with non-readonly members
- Exclusion of members containing `byref` parameters from code generation to avoid FS0412 and FS3300 errors
### Removed
- Code generation for mutable instance fields

## [0.1.0] - 2020-09-06
### Added
- Basic code generation for all dependencies
