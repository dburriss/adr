namespace adr

open System
open System.Collections.Generic
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
    module FileSystem =
        let rootDir = DirectoryPath "/Working"