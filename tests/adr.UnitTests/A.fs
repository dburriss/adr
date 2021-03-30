namespace adr

open Spectre.IO
open Spectre.IO.Testing

type FileSystemBuilder() =
    let env = FakeEnvironment.CreateUnixEnvironment()
    let fs = FakeFileSystem(env)
    
    member this.WithFile (filePath) =
        fs.CreateFile(FilePath filePath) |> ignore
        this
    
    member this.WithFile(filePath, content) =
        fs.CreateFile(FilePath filePath).SetTextContent(content) |> ignore
        this

    member this.WithDir (dirPath) =
        fs.CreateDirectory(DirectoryPath dirPath) |> ignore
        this
        
        
    member this.Build() = fs
    
module A =
    module Adr =
        [<Literal>]
        let defaultMarkdown = """# 1. Record architecture decisions

Date: 2020-12-29

## Status

Proposed

## Context

What is the issue that we're seeing that is motivating this decision or change?

## Decision

What is the change that we're proposing and/or doing?

## Consequences

What becomes easier or more difficult to do because of this change?

"""

        let content = ("0001-record-architecture-decisions", defaultMarkdown)
    module FileSystem =
        let rootDir = DirectoryPath "/Working"
        let initializedProjectFilesystem =
            FileSystemBuilder()
                .WithDir("/Working/docs/adr/")
                .WithFile("/Working/.adr-dir", "docs/adr")
                .WithFile("/Working/docs/adr/0001-record-architecture-decisions.md", Adr.defaultMarkdown)
                .Build()
                
    module Project =
        let initializedProject =
            Project(FileSystem.initializedProjectFilesystem, FileSystem.rootDir)