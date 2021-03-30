module AdrTests

open System
open adr
open Xunit
open Swensen.Unquote

[<Fact>]
let ``Adr content gets number``() =
    let adr = Adr.fromContent A.Adr.content
    test <@ adr.Number = 1 @>
    
[<Fact>]
let ``Adr content gets title``() =
    let adr = Adr.fromContent A.Adr.content
    test <@ adr.Title = "Record architecture decisions" @>
        
[<Fact>]
let ``Adr content gets date``() =
    let adr = Adr.fromContent A.Adr.content
    test <@ adr.DateTime.Date = DateTime.Parse("2020-12-29") @>
         
[<Fact>]
let ``Adr supersede changes old adr``() =
    let oldAdr = Adr.fromContent A.Adr.content
    let newAdr = Adr.newAdr oldAdr.Number "A new ADR" DateTime.Now
    let (oldAdr', newAdr') = Adr.supersede oldAdr newAdr
    Assert.NotEqual(oldAdr, oldAdr')
         
[<Fact>]
let ``Adr supersede changes new adr``() =
    let oldAdr = Adr.fromContent A.Adr.content
    let newAdr = Adr.newAdr oldAdr.Number "A new ADR" DateTime.Now
    let (oldAdr', newAdr') = Adr.supersede oldAdr newAdr
    Assert.NotEqual(newAdr, newAdr')
    
    