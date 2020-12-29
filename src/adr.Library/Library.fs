namespace adr

open System
open System.Diagnostics
open Spectre.IO
open Markdig

// DONE | Init : First time ADR
// Init : Change ADR dir

// New : with no .adr-dir
// DONE | New : ADR
// New : ADR that supersedes
// New : link

// list

// link

// config ?

// generate

// interactive

type Adr = {
    Number : int
    Title : string
    Content : string
    FileNameSansExt : string
}

type AdrRepository(fileSystem : IFileSystem, root : DirectoryPath, pathOpt : DirectoryPath option) =
    static let adrDirFilename = ".adr-dir"
    static let seekHeight = 10
    static let readContent (file : IFile) =
        use stream = file.OpenRead()
        use reader = new IO.StreamReader(stream)
        reader.ReadToEnd()
    
    static let writeContent (file : IFile) (content : string) =
        use stream = file.OpenWrite()
        use reader = new IO.StreamWriter(stream)
        reader.Write(content)
        
    let adrDirectoryPath = defaultArg pathOpt (root.Combine(DirectoryPath "docs/adr/"))
    member val SeekHeight = seekHeight with get, set
    
    static member TryFindAdrDirFile(fileSystem : IFileSystem, path : DirectoryPath) =
        let folders = fileSystem.GetDirectory(path).Path.Segments |> Seq.toArray
        let nrItems = folders.Length
        let rec seek upCount =
            let dir = Array.take (nrItems - upCount) folders |> IO.Path.Combine |> DirectoryPath
            Debug.WriteLine(sprintf "Searching %s for %s" dir.FullPath adrDirFilename)
            let file = dir.GetFilePath(FilePath adrDirFilename)
            
            if upCount > nrItems || upCount > seekHeight then
                None
            elif fileSystem.Exist(file) then
                Debug.WriteLine(sprintf "Found %s in %s" adrDirFilename dir.FullPath )
                Some (fileSystem.GetFile(FilePath adrDirFilename))
            else seek (upCount+1)
        seek 0
        
    static member GetAdrPathFromFile(fileSystem : IFileSystem, file : IFile) =
        if fileSystem.Exist file.Path then
            let content = readContent file
            DirectoryPath (content.Trim()) |> Some
        else None
        
    static member Create(fileSystem : IFileSystem, root : DirectoryPath, passedAdrDir : DirectoryPath option) =
        AdrRepository.TryFindAdrDirFile(fileSystem, root)
        |> Option.bind(fun file -> AdrRepository.GetAdrPathFromFile(fileSystem, file))
        |> fun dirPathFromFile ->
            match (dirPathFromFile, passedAdrDir) with
            | None, None -> AdrRepository(fileSystem, root, None)
            | Some a, None -> AdrRepository(fileSystem, root, Some a)
            | None, Some b -> AdrRepository(fileSystem, root, Some b)
            | Some a, Some b -> AdrRepository(fileSystem, root, Some a) // Favours file. Expected?
            
    member this.HasAdrDirFile() =
        AdrRepository.TryFindAdrDirFile(fileSystem, root) |> Option.isSome
        
    member this.HasAdrDir() =
        AdrRepository.TryFindAdrDirFile(fileSystem, root)
        |> Option.bind (fun file -> AdrRepository.GetAdrPathFromFile(fileSystem, file))
        |> Option.map (fun dir -> fileSystem.Exist(dir))
        |> Option.defaultValue false
        
    member this.AdrDir() =
        AdrRepository.TryFindAdrDirFile(fileSystem, root)
        |> Option.bind (fun file -> AdrRepository.GetAdrPathFromFile(fileSystem, file))
        |> Option.map (fun file -> file.ToString())
        |> Option.defaultValue ""
        
    member this.Init() =
        
        // could create the file with tasks to do the steps
        Debug.WriteLine(sprintf "Init: Root is %s, path is %s" (root.FullPath) (adrDirectoryPath.FullPath))
        // create dir
        if not(fileSystem.Exist adrDirectoryPath) then
            Debug.WriteLine(sprintf "Init: Creating directory %s" adrDirectoryPath.FullPath)
            fileSystem.Directory.Create(adrDirectoryPath)
            
        let filePath = root.CombineWithFilePath(FilePath adrDirFilename)
        
        // create file
        if(not(fileSystem.Exist filePath)) then
            Debug.WriteLine(sprintf "Init: Creating file %s" (filePath.FullPath))
            let content = root.GetRelativePath(adrDirectoryPath).ToString()
            Debug.WriteLine(sprintf "Init: adr-dir is %s" content)
            writeContent (fileSystem.GetFile(filePath)) content
        
    member this.Files(?search) =
        let glob = defaultArg search "*.md"
        fileSystem.GetDirectory(adrDirectoryPath).GetFiles(glob, SearchScope.Current)
    
    member this.GetLast() =
        this.Files()
        |> Seq.sortByDescending (fun file -> file.Path.GetFilenameWithoutExtension().ToString())
        |> Seq.tryHead
    
    member this.GetByNumber(nr : int) =
        let search = sprintf "%04i-*" nr
        this.Files(search) |> Seq.head
        
    member this.WriteAdr(title, content) =
        let filePath = adrDirectoryPath.GetFilePath(FilePath (sprintf "%s.md" title))
        if(fileSystem.Exist(filePath)) then
            fileSystem.File.Delete(filePath)
        let file = fileSystem.GetFile(filePath)
        writeContent file content
        filePath

module String =
    let splitByChar (seps : char array) (s : string) = s.Split(seps)
    let replace (oldValue : string) (newValue : string) (s : string) =
        s.Replace(oldValue, newValue) 
    let lower (s : string) = s.ToLowerInvariant()

/// Creating Adr documents
module Adr =
    
    open FuncyDown.Document
    
    let private fileName (file : IFile) = file.Path.GetFilenameWithoutExtension().ToString()
    let private extractNr (fileName : string) =
        String.splitByChar [|'-'|] fileName
        |> Array.tryHead
        |> Option.map (Convert.ToInt32)
    let private slug = String.lower >> String.replace " " "-"
    let private toNrString = sprintf "%04i"
    let newAdr (file : IFile option) title =
        let lastNr = file |> Option.bind (fileName >> extractNr) |> Option.defaultValue 0
        let nextNr = lastNr+1
        let codeAndTitle = sprintf "%s-%s" (toNrString nextNr) (slug title)
        
        let content =
            emptyDocument
            |> addH1 title
            |> addH2 "Status"
            |> addParagraph "Proposed"
            |> addH2 "Context"
            |> addParagraph "What is the issue that we're seeing that is motivating this decision or change?"
            |> addH2 "Decision"
            |> addParagraph "What is the change that we're proposing and/or doing?"
            |> addH2 "Consequences"
            |> addParagraph "What becomes easier or more difficult to do because of this change?"
            |> asString
            
        {
            Title = title
            Number = nextNr
            Content = content
            FileNameSansExt = codeAndTitle
        }
    
    let supersede (file : FilePath) (by : Adr) = ()
        