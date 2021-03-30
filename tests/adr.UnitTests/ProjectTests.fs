module ProjectTests

open Spectre.IO
open adr
open Xunit
open Swensen.Unquote
open System

[<Fact>]
let ``create Project when no adr-dir file throws``() =
    let fileSystem = FileSystemBuilder().Build()
    let root = A.FileSystem.rootDir
    let action = Action(fun () -> Project.create fileSystem root 5 |> ignore)
    Assert.Throws<AdrDirFileNotFoundException>(action)
    
[<Fact>]
let ``Project when no adr-dir file throws``() =
    let fileSystem = FileSystemBuilder().Build()
    let root = A.FileSystem.rootDir
    let action = Action(fun () -> Project(fileSystem, root) |> ignore)
    Assert.Throws<AdrDirFileNotFoundException>(action)
    
[<Fact>]
let ``create Project when adr-dir is further than seekHeight throws``() =
    let adrFile = "/Working/.adr-dir"
    let currentPath = "/Working/one/two/three"
    let fileSystem = FileSystemBuilder()
                         .WithDir(currentPath)
                         .WithFile(adrFile, "/Working/path/to/adr/").Build()
    let root = DirectoryPath currentPath
    let action = Action(fun () -> Project.create fileSystem root 1 |> ignore)
    Assert.Throws<AdrDirFileNotFoundException>(action)
     
[<Fact>]
let ``create Project when adr-dir is empty throws``() =
    let adrFile = "/Working/.adr-dir"
    let currentPath = "/Working"
    let fileSystem = FileSystemBuilder()
                         .WithDir(currentPath)
                         .WithFile(adrFile).Build()
    let root = DirectoryPath currentPath
    let action = Action(fun () -> Project.create fileSystem root 1 |> ignore)
    Assert.Throws<AdrDirFilePathNotFoundException>(action)
         
[<Fact>]
let ``Project returns correct adr path``() =
    let adrFile = "/Working/.adr-dir"
    let adrPath = "path/to/adr"
    let currentPath = "/Working"
    let fileSystem = FileSystemBuilder()
                         .WithDir(currentPath)
                         .WithFile(adrFile, adrPath).Build()
    let root = DirectoryPath currentPath
    let project = Project(fileSystem, root)
    test <@ project.AdrPath().FullPath = (DirectoryPath adrPath).FullPath @>
    
[<Fact>]
let ``Project init with no path uses default``() =
    let currentPath = "/Working"
    let adrPath = "docs/adr"
    let fileSystem = FileSystemBuilder().WithDir("/Working").Build()
    let root = DirectoryPath currentPath
    do Project.init fileSystem root None |> ignore
    let project = Project(fileSystem, root)
    
    test <@ project.AdrPath().FullPath = adrPath @>
    
[<Fact>]
let ``Find First for non-existent file glob returns None``() =
    let project = A.Project.initializedProject
    
    test <@ project.TryFindFirst("9999-*.md").IsNone = true @>
    
[<Fact>]
let ``FindFirst Project Files with 1 file returns 1``() =
    let project = A.Project.initializedProject
    
    test <@ project.TryFindFirst("0001-*.md").IsSome = true @>
    
[<Fact>]
let ``Get Project Files with 1 file returns 1``() =
    let project = A.Project.initializedProject
    
    test <@ project.Get(Int32.MaxValue) |> Seq.length = 1 @>
    
