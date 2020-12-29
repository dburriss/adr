namespace adr

open Spectre.IO
open System
open System.Diagnostics

type AdrDirFileNotFoundException = exn
type AdrDirFilePathNotFoundException = exn

/// Represents a project with an initialized .adr-dir
/// `root` directory is expected to have an existing .adr-dir file with a path contained within.
type Project(fileSystem : IFileSystem, root : DirectoryPath) =
    static let adrDirFilename = ".adr-dir"
    let mutable adrPath : DirectoryPath option = None
    
    let readContent (file : IFile) =
        use stream = file.OpenRead()
        use reader = new IO.StreamReader(stream)
        reader.ReadToEnd()
    
    let writeContent (file : IFile) (content : string) =
        use stream = file.OpenWrite()
        use reader = new IO.StreamWriter(stream)
        reader.Write(content)
        
    let getAdrPathFromFile (fileSystem : IFileSystem) (file : IFile) =
        if fileSystem.Exist file.Path then
            let content = readContent file
            DirectoryPath (content.Trim()) |> Some
        else None
        
    do
        let filePath = root.GetFilePath(FilePath adrDirFilename)
        if not(fileSystem.Exist(filePath)) then
            let msg = sprintf "No `.adr-dir` file in %s." (filePath.GetDirectory().FullPath)
            raise (AdrDirFileNotFoundException msg)
        let file = fileSystem.GetFile(filePath)
        adrPath <- getAdrPathFromFile fileSystem file
        if(adrPath.IsNone) then
            let msg = sprintf "`.adr-dir` file in %s contained no path to ADRs." (filePath.GetDirectory().FullPath)
            raise (AdrDirFilePathNotFoundException msg)
            
    member this.AdrPath() = adrPath.Value
    
    member this.Files(?search) =
        let glob = defaultArg search "*.md"
        fileSystem.GetDirectory(this.AdrPath()).GetFiles(glob, SearchScope.Current)
    
    member this.GetLast() =
        this.Files()
        |> Seq.sortByDescending (fun file -> file.Path.GetFilenameWithoutExtension().ToString())
        |> Seq.tryHead
        
    member this.WriteContent(fileNameSansExt, content) =
        let filePath = this.AdrPath().GetFilePath(FilePath (sprintf "%s.md" fileNameSansExt))
        if(fileSystem.Exist(filePath)) then
            fileSystem.File.Delete(filePath)
        let file = fileSystem.GetFile(filePath)
        writeContent file content
        file
        
module Project =
    [<Literal>]
    let adrDirFilename = ".adr-dir"
        
    let tryFindAdrDirFile(fileSystem : IFileSystem) (path : DirectoryPath) (seekHeight) =
        let folders = fileSystem.GetDirectory(path).Path.Segments |> Seq.toArray
        let nrItems = folders.Length
        let rec seek upCount =
            let dir = Array.take (nrItems - upCount) folders |> IO.Path.Combine |> DirectoryPath
            Debug.WriteLine(sprintf "Searching %s for %s" dir.FullPath adrDirFilename)
            let file = dir.GetFilePath(FilePath adrDirFilename)
            
            if upCount >= nrItems || upCount >= seekHeight then
                None
            elif fileSystem.Exist(file) then
                Debug.WriteLine(sprintf "Found %s in %s" adrDirFilename dir.FullPath )
                Some (fileSystem.GetFile(FilePath adrDirFilename))
            else seek (upCount+1)
        seek 0
        
    /// Creates a project instance by searching for .adr-dir file and using that directory as root.
    /// Throws if no file found within the seek height.
    let create (fileSystem : IFileSystem) (current : DirectoryPath) (seekHeight) =
        tryFindAdrDirFile fileSystem current seekHeight
        |> Option.map(fun file -> Project(fileSystem, file.Path.GetDirectory()))
        |> Option.defaultWith(fun () ->
            let msg = sprintf "No directory found with `.adr-dir` seeking up %i from %s" seekHeight (current.GetDirectoryName())
            raise (AdrDirFileNotFoundException msg))
        
    let init (fileSystem : IFileSystem) (root : DirectoryPath) (path : string) =
        let adrDirectoryPath = DirectoryPath path
        // could create the file with tasks to do the steps
        Debug.WriteLine(sprintf "Init: Root is %s, path is %s" (root.FullPath) path)
            
        let filePath = root.CombineWithFilePath(FilePath adrDirFilename)
        
        // create file
        if(not(fileSystem.Exist filePath)) then
            Debug.WriteLine(sprintf "Init: Creating file %s" (filePath.FullPath))
            let content = root.GetRelativePath(adrDirectoryPath).ToString()
            Debug.WriteLine(sprintf "Init: adr-dir is %s" content)
        let file = fileSystem.GetFile(filePath)
        use stream = file.OpenWrite()
        use reader = new IO.StreamWriter(stream)
        reader.Write(path)
                
        // create dir
        if not(fileSystem.Exist adrDirectoryPath) then
            Debug.WriteLine(sprintf "Init: Creating directory %s" adrDirectoryPath.FullPath)
            fileSystem.Directory.Create(adrDirectoryPath)
        
        Project(fileSystem, root)
