module AdrRepositoryTests

open Spectre.IO
open adr
open Xunit
open Swensen.Unquote
open System

[<Fact>]
let ``Top ADR on new is 1``() =
    let adrRepository = AdrRepository(A.Project.initializedProject)
    let top = adrRepository.TopNumber()
    <@ top = 1 @>
 
[<Fact>]
let ``Get first ADR title matches``() =
    let adrRepository = AdrRepository(A.Project.initializedProject)
    let adr = adrRepository.GetByNumber(1)
    <@ adr.Title = "Record architecture decisions" @>
    
 
[<Fact>]
let ``Get first ADR Number is 1``() =
    let adrRepository = AdrRepository(A.Project.initializedProject)
    let adr = adrRepository.GetByNumber(1)
    <@ adr.Number = 1 @>
    
[<Fact>]
let ``Get first ADR content is default template``() =
    let adrRepository = AdrRepository(A.Project.initializedProject)
    let adr = adrRepository.GetByNumber(1)
    <@ adr.Content = A.Adr.defaultMarkdown @>
    
    