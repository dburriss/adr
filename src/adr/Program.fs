open System
open Argu
open Spectre.IO
open adr

type InitArgs =
    | [<MainCommand; ExactlyOnce; Last>] Path of path_to_adr_directory:string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Path _ -> "The path where ADRs will be created."
            
type NewArgs =
    | [<MainCommand; ExactlyOnce; Last>] Title of title_text:string
    | [<AltCommandLine("-s")>] Supersedes of number_that_is_superseded:int

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Title _ -> "The title of the new decision record."
            | Supersedes _ -> "The number of the decision record that is being replaced."
            
type AdrCommands =
    | [<CliPrefix(CliPrefix.None)>] Init of ParseResults<InitArgs>
    | [<CliPrefix(CliPrefix.None)>] New of ParseResults<NewArgs>
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init _ -> "Creates the specified folder and first ADR. \nThe path to ADRs is stored in `.adr-dir` in the directory where `adr` command is called."
            | New _ -> "Creates a new ADR with an incremented number and the title text. \nCan supersede an existing ADR."

type InitSettings = {
    AdrPath : DirectoryPath option
}

type NewSettings = {
    Title : string
    Supersedes : int option
}

[<EntryPoint>]
let main argv =
    let fileSystem = FileSystem()
    let currentDir = DirectoryPath (Environment.CurrentDirectory)
    let errorHandler =
        ProcessExiter(
            colorizer =
                function
                | ErrorCode.HelpText -> None
                | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<AdrCommands>(programName = "[dotnet] adr", errorHandler = errorHandler)
    //------------------------------------------------------------------------------------------------------------------
    // Commands setup
    //------------------------------------------------------------------------------------------------------------------
    let (|Help|_|) (result : ParseResults<AdrCommands>) : string option =
        if(result.IsUsageRequested) then Some (parser.PrintUsage())
        else
            let initCmd = result.TryGetResult AdrCommands.Init
            let newCmd = result.TryGetResult AdrCommands.New
            if(initCmd.IsSome && initCmd.Value.IsUsageRequested) then
                Some (initCmd.Value.Parser.PrintUsage())
            elif(newCmd.IsSome && newCmd.Value.IsUsageRequested) then
                Some (newCmd.Value.Parser.PrintUsage())
            else None
        
    let (|InitCommand|_|) (result : ParseResults<AdrCommands>) : InitSettings option =
        let cmd = result.TryGetResult AdrCommands.Init
        match cmd with
        | None -> None
        | Some initArgs ->
            Some {
                AdrPath = initArgs.TryGetResult InitArgs.Path |> Option.map DirectoryPath
            }
    
    let (|NewCommand|_|) (result : ParseResults<AdrCommands>) : NewSettings option =
        let cmd = result.TryGetResult AdrCommands.New
        match cmd with
        | None -> None
        | Some newArgs ->
            Some {
                Title = newArgs.GetResult NewArgs.Title
                Supersedes = newArgs.TryGetResult NewArgs.Supersedes
            }
            
    //------------------------------------------------------------------------------------------------------------------
    // Execute
    //------------------------------------------------------------------------------------------------------------------
    try
        let result = parser.ParseCommandLine(inputs = argv, raiseOnUsage = false)
        match result with
        | Help usage -> Console.WriteLine(usage)
        | InitCommand initSettings ->
            printfn "Initializing..."
            let repo = AdrRepository(fileSystem, currentDir, initSettings.AdrPath)
            repo.Init()
            if(repo.Files() |> Seq.isEmpty) then
                let (codeAndTitle, content) = Adr.newAdr repo "record-architecture-decisions"
                let file = repo.WriteAdr(codeAndTitle, content)
                printfn "created %s" (file.ToString())
                
        | NewCommand newSettings ->
            printfn "New ADR..."
            let repo = AdrRepository.Create(fileSystem, currentDir, None)
            if(not(repo.HasAdrDirFile())) then
                failwithf "Missing `.adr-dir`. Run `adr init /path/to/adr/dir/location`."
            if(not(repo.HasAdrDir())) then
                failwithf "Missing directory %s" (repo.AdrDir())
            // new adr
            let adr = Adr.newAdr (repo.GetLast()) newSettings.Title
            let file = repo.WriteAdr(adr.FileNameSansExt, adr.Content)
            // supersede
            newSettings.Supersedes |> Option.iter Adr.supersede
            printfn "created %s" (file.ToString())
            
        | _ -> parser.PrintUsage() |> Console.WriteLine
        0 // return an integer exit code
    with
    | ex ->
        let c = Console.ForegroundColor
        Console.ForegroundColor <- ConsoleColor.Red
        Console.WriteLine(ex.Message)
        Console.ForegroundColor <- c
        parser.PrintUsage() |> ignore
        -1
