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
            
type ListArgs =
    | [<AltCommandLine("-n")>] Number of number_items_to_list:int

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Number _ -> "Limit to n number of records when listing."
            
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
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ListArgs>
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init _ -> "Creates the specified folder and first ADR. \nThe path to ADRs is stored in `.adr-dir` in the directory where `adr` command is called."
            | New _ -> "Creates a new ADR with an incremented number and the title text. \nCan supersede an existing ADR."
            | List _ -> "List existing ADRs."

type InitSettings = {
    AdrPath : string option
}

type NewSettings = {
    Title : string
    Supersedes : int option
}

type ListSettings = {
    Number : int option
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
            let listCmd = result.TryGetResult AdrCommands.List
            if(initCmd.IsSome && initCmd.Value.IsUsageRequested) then
                Some (initCmd.Value.Parser.PrintUsage())
            elif(newCmd.IsSome && newCmd.Value.IsUsageRequested) then
                Some (newCmd.Value.Parser.PrintUsage())
            elif(listCmd.IsSome && listCmd.Value.IsUsageRequested) then
                Some (listCmd.Value.Parser.PrintUsage())
            else None
        
    let (|InitCommand|_|) (result : ParseResults<AdrCommands>) : InitSettings option =
        let cmd = result.TryGetResult AdrCommands.Init
        match cmd with
        | None -> None
        | Some initArgs ->
            Some {
                AdrPath = initArgs.TryGetResult InitArgs.Path
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
            
    let (|ListCommand|_|) (result : ParseResults<AdrCommands>) : ListSettings option =
        let cmd = result.TryGetResult AdrCommands.List
        match cmd with
        | None -> None
        | Some listArgs ->
            Some {
                Number = listArgs.TryGetResult ListArgs.Number
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
            let proj = Project.init fileSystem currentDir initSettings.AdrPath
            if(proj.HasNoFiles()) then
                let repo = AdrRepository(proj)
                let adr = Adr.newAdr (repo.TopNumber()) " Record architecture decisions"
                let file = repo.WriteAdr(adr)
                printfn "created %i - %s" adr.Number adr.Title
            else printfn "Already initialized. Doing nothing."
                
        | NewCommand newSettings ->
            printfn "New ADR..."
            let proj = Project.create fileSystem currentDir 10
            let repo = AdrRepository(proj)
            // new adr
            let adr = Adr.newAdr (repo.TopNumber()) newSettings.Title
            // supersede
            // change old adr - update status and add link to new adr
            
            printfn "created %i - %s" adr.Number adr.Title
                   
        | ListCommand listSettings ->
            printfn "List ADRs..."
            let proj = Project.create fileSystem currentDir 10
            let repo = AdrRepository(proj)
            // list adrs
            let adrs = repo.GetAdrs(listSettings.Number)
            adrs |> Seq.iter (fun adr -> printfn "%i - %s" adr.Number adr.Title)
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
