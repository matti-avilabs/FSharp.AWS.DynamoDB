﻿namespace FSharp.AWS.DynamoDB.Tests

open System
open System.Threading

open Expecto

open FSharp.AWS.DynamoDB

[<AutoOpen>]
module SimpleTableTypes =

    type SimpleRecord =
        {
            [<HashKey>]
            HashKey : string
            [<RangeKey>]
            RangeKey : string

            Value : int64

            Tuple : int64 * int64

            Map : Map<string, int64>

            Unions : Choice<string, int64, byte[]> list
        }

    [<ConstantHashKeyAttribute("HashKey", "compatible")>]
    type CompatibleRecord =
        {
            [<RangeKey; CustomName("RangeKey")>]
            Id : string

            Values : Set<int>
        }

    [<ConstantRangeKeyAttribute("RangeKey", "compatible")>]
    type CompatibleRecord2 =
        {
            [<HashKey; CustomName("HashKey")>]
            Id : string

            Values : Set<int>
        }

type ``Simple Table Operation Tests`` (fixture : TableFixture) =

    let rand = let r = Random() in fun () -> int64 <| r.Next()
    let mkItem() = 
        { 
            HashKey = guid() ; RangeKey = guid() ; 
            Value = rand() ; Tuple = rand(), rand() ;
            Map = seq { for i in 0L .. rand() % 5L -> "K" + guid(), rand() } |> Map.ofSeq 
            Unions = [Choice1Of3 (guid()) ; Choice2Of3(rand()) ; Choice3Of3(Guid.NewGuid().ToByteArray())]
        }

    let table = TableContext.Create<SimpleRecord>(fixture.Client, fixture.TableName, createIfNotExists = true)

    member this.``Convert to compatible table`` () =
        let table' = table.WithRecordType<CompatibleRecord> ()
        Expect.equal table'.PrimaryKey table.PrimaryKey "PrimaryKey should be equal" 

    member this.``Convert to compatible table 2`` () =
        let table' = table.WithRecordType<CompatibleRecord2> ()
        Expect.equal table'.PrimaryKey table.PrimaryKey "PrimaryKey should be equal" 

    member this.``Simple Put Operation`` () =
        let value = mkItem()
        let key = table.PutItem value
        let value' = table.GetItem key
        Expect.equal value' value "value should be equal"

    member this.``ContainsKey Operation`` () =
        let value = mkItem()
        let key = table.PutItem value
        Expect.equal (table.ContainsKey key) true "ContainsKey should be true"
        let _ = table.DeleteItem key
        Expect.equal (table.ContainsKey key) false "ContainsKey should be false"

    member this.``Batch Put Operation`` () =
        let values = set [ for i in 1L .. 20L -> mkItem() ]
        let keys = table.BatchPutItems values
        let values' = table.BatchGetItems keys |> Set.ofArray
        Expect.equal values' values "values should be equal"

    member this.``Simple Delete Operation`` () =
        let item = mkItem()
        let key = table.PutItem item
        Expect.equal (table.ContainsKey key) true "ContainsKey should be true"
        let item' = table.DeleteItem key
        Expect.equal item' item "item should be equal"
        Expect.equal (table.ContainsKey key) false "ContainsKey should be false"