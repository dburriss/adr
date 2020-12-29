namespace adr

open Spectre.IO
open System
open System.Diagnostics

type AdrDirFileNotFoundException = exn
type AdrDirFilePathNotFoundException = exn

/// Responsible for interacting with filesystem and ensuring correct files are in correct place.
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
    
    let mapFile (file : IFile) = file.Path.GetFilenameWithoutExtension().ToString(), file
    let mapFileAndContent (name, file : IFile) = name, readContent file
    member this.AdrPath() = adrPath.Value
    
    member private this.Files(?search) =
        let glob = defaultArg search "*.md"
        let adrPath = this.AdrPath()
        let adrDir = root.Combine(adrPath)
        let adrFiles = fileSystem.GetDirectory(adrDir).GetFiles("0001-*.md", SearchScope.Current) |> Seq.map mapFile
        adrFiles
    
    member this.HasNoFiles() = this.Files() |> Seq.isEmpty
    
    member this.GetLastTitle() =
        this.Files()
        |> Seq.sortByDescending fst
        |> Seq.tryHead
        |> Option.map fst
        
    member this.TryFindFirst(search) =
        this.Files(search)
        |> Seq.tryHead
        |> Option.map mapFileAndContent
        
    member this.Get(n) =
        this.Files() |> Seq.truncate n
        |> Seq.map mapFileAndContent
        
    member this.WriteContent(fileName, content) =
        let filePath = this.AdrPath().GetFilePath(FilePath fileName)
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
        
    let init (fileSystem : IFileSystem) (root : DirectoryPath) (path : string option) =
        let adrPathS = Option.defaultValue "docs/adr/" path
        let adrDirectoryPath = root.Combine(DirectoryPath.FromString(adrPathS))
        // could create the file with tasks to do the steps
        Debug.WriteLine(sprintf "Init: Root is %s, path is %s" (root.FullPath) adrPathS)
            
        let filePath = root.CombineWithFilePath(FilePath adrDirFilename)
        
        // create file
        if(not(fileSystem.Exist filePath)) then
            Debug.WriteLine(sprintf "Init: Creating file %s" (filePath.FullPath))
            let content = root.GetRelativePath(adrDirectoryPath).ToString()
            Debug.WriteLine(sprintf "Init: adr-dir is %s" content)
            let file = fileSystem.GetFile(filePath)
            use stream = file.OpenWrite()
            use reader = new IO.StreamWriter(stream)
            reader.Write(content)
        else printfn "%s already exists." filePath.FullPath
                
        // create dir
        if not(fileSystem.Exist adrDirectoryPath) then
            Debug.WriteLine(sprintf "Init: Creating directory %s" adrDirectoryPath.FullPath)
            fileSystem.Directory.Create(adrDirectoryPath)
        else printfn "%s already exists." adrDirectoryPath.FullPath
        
        Project(fileSystem, root)
