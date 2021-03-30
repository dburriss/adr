namespace adr

open System

type Adr = {
    Number : int
    Title : string
    Content : string
    DateTime : DateTime
}

/// Creating Adr documents
module Adr =
    
    open System
    open FuncyDown
    open FuncyDown.Document
    open FuncyDown.Element
    open Markdig
    open Markdig.Syntax

    let extractNrFromFilename (fileName : string) =
        String.splitByChar [|'-'|] fileName
        |> Array.tryHead
        |> Option.map (Convert.ToInt32)
        
    let private formatSlug = String.lower >> String.replace " " "-"
    let private mapToFilename fileNameSansExt = (sprintf "%s.md" fileNameSansExt)
    let adrNumberAsString i = sprintf "%04i" i
    let private mapToFileNameSansExt adr = sprintf "%s-%s" (adrNumberAsString adr.Number) (formatSlug adr.Title)
    let adrToFilename = mapToFileNameSansExt >> mapToFilename
        
    let parse content =
        Markdown.Parse(content)
        
    let extractTitleFromContent (fileName, content) (doc : MarkdownDocument) =
        let headingBlock = doc.Descendants<HeadingBlock>() |> Seq.tryHead
        let number = extractNrFromFilename fileName |> Option.defaultValue 0
        match headingBlock with
        | None -> ""
        | Some heading ->
            let mdTitle = String.substring (heading.Span.Start+1) (heading.Span.Length-1) content
            let title = mdTitle |> String.replace (sprintf "%i." number) "" |> String.trim
            title
        
    let extractDateFromContent (_, content) (doc : MarkdownDocument) =
        let paragraphBlock = doc.Descendants<ParagraphBlock>() |> Seq.tryHead
        match paragraphBlock with
        | None -> None
        | Some p ->
            let dt = String.substring (p.Span.Start+5) (p.Span.Length-5) content
            Some (Convert.ToDateTime(dt))
        
    let fromContent (fileName, content) =
        let doc = parse content
        let title = extractTitleFromContent (fileName,content) doc
        let number = extractNrFromFilename fileName |> Option.defaultValue 0
        let dt = extractDateFromContent (fileName,content) doc |> Option.defaultValue DateTime.MinValue
        {
            Title = title
            Number = number
            Content = content
            DateTime = dt
        }
    
    let newAdr lastNr titleTxt (date : DateTime) =
        let nextNr = lastNr+1
        let title = sprintf "%i. %s" nextNr (String.trim titleTxt)
        let dtS = sprintf "Date: %s" (date.ToShortDateString())
        let content =
            emptyDocument
            |> addH1 title
            |> addParagraph dtS
            |> addH2 "Status"
            |> addParagraph "Accepted"
            |> addH2 "Context"
            |> addParagraph "What is the issue that we're seeing that is motivating this decision or change?"
            |> addH2 "Decision"
            |> addParagraph "What is the change that we're proposing and/or doing?"
            |> addH2 "Consequences"
            |> addParagraph "What becomes easier or more difficult to do because of this change?"
            |> Document.asString
            
        {
            Title = title
            Number = nextNr
            Content = content
            DateTime = date
        }
    
    let private addSupercededBy (adr : Adr) : Transform =
        fun ctx el ->
            match el with
            | Header h when h.Size = H2 && h.Text = "Status" ->
                ctx.Set<bool>(true) ; [el] // must return current element
            | Paragraph text ->
                match (ctx.Get<bool>()) with
                | Some true ->
                    let fileNameWithExt = adrToFilename adr
                    let text = sprintf "Superceded by [%i. %s](%s)" adr.Number adr.Title fileNameWithExt
                    ctx.Set<bool>(false)
                    [Paragraph text]
                | _ -> [el]
            | _ -> [el]
    let supersede (old : Adr) (by : Adr) =
        // change old
        let oldDoc = Document.parse old.Content
        let addSupercededByNewAdr = addSupercededBy by 
        let oldtransformations = [addSupercededByNewAdr]
        let old' = { old with Content = Transformer.transform oldtransformations oldDoc |> Document.asString }
        (old', by)
