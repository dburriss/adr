namespace adr

open System

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

/// Responsible for mapping between ADR and the files
type AdrRepository(project : Project) =
    static let seekHeight = 10
    let formatSlug = String.lower >> String.replace " " "-"
    let adrNumberAsString i = sprintf "%04i" i
    let mapToFilename fileNameSansExt = (sprintf "%s.md" fileNameSansExt)
    let mapToFileNameSansExt adr = sprintf "%s-%s" (adrNumberAsString adr.Number) (formatSlug adr.Title)
    let adrToFilename = mapToFileNameSansExt >> mapToFilename

    member this.TopNumber() =
        project.GetLastTitle()
        |> Option.bind Adr.extractNrFromFilename
        |> Option.defaultValue 0
    
    member this.GetByNumber(nr : int) =
        let adrNumberString = (adrNumberAsString nr)
        let search = sprintf "%s-*.md " adrNumberString
        match project.TryFindFirst(search) with
        | None -> failwithf "ADR %s not found." adrNumberString
        | Some c -> Adr.fromContent c
            
    // Gets n number of ADRs
    member this.GetAdrs(n) =
        let take = Option.defaultValue Int32.MaxValue n
        project.Get(take)
        |> Seq.map Adr.fromContent
        
    member this.WriteAdr(adr : Adr) =
        let fileName = adrToFilename adr
        do project.WriteContent(fileName, adr.Content) |> ignore
        adr


        