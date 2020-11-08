# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Copying `ObsoleteAttribute` and `ExperimentalAttribute` on correponding modules and members
- Suppression of warnings FS0044, FS0057, and FS0064 in generated code
- Generation of functions for read-only instance fields
- Code generation target now only runs for F# (`.fsproj`) projects
- Now compatible with `<ProjectReference>` dependencies
- Exclusion of members marked `ObsoleteAttribute` when `IsError` is `true` to avoid errors that cannot be suppressed
- Exclusion of structs not marked with `IsReadOnlyAttribute` from code generation to avoid problems with non-readonly members

## [0.1.0] - 2020-09-06
### Added
- Basic code generation for all dependencies
