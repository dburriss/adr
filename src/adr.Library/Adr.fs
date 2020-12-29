namespace adr

type Adr = {
    Number : int
    Title : string
    Content : string
}

/// Creating Adr documents
module Adr =
    
    open System
    open FuncyDown.Document
    open Markdig
    open Markdig.Syntax

    let extractNrFromFilename (fileName : string) =
        String.splitByChar [|'-'|] fileName
        |> Array.tryHead
        |> Option.map (Convert.ToInt32)
        
    let parse content =
        Markdown.Parse(content)
        
    let extractTitleFromContent (content : string) (doc : MarkdownDocument) =
        let headingBlock = doc.Descendants<HeadingBlock>() |> Seq.tryHead
        match headingBlock with
        | None -> ""
        | Some heading ->
            content.Substring(heading.Span.Start+1, heading.Span.Length).Trim()
        
    let fromContent (fileName, content) =
        {
            Title = content
            Number = extractNrFromFilename fileName |> Option.defaultValue 0
            Content = content
        }
    
    let newAdr lastNr title =
        let nextNr = lastNr+1
        
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
        }
    
    let supersede (old : Adr) (by : Adr) =
        old
