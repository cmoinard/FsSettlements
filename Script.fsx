#r "nuget: FsHttp"
#r "nuget: Newtonsoft.Json"

open System
open FsHttp
open FsHttp.DslCE
open FSharp.Data
open Newtonsoft.Json

let baseUrl = "http://localhost:8085"
let withoutPagingUrl = baseUrl
let withPagingUrl = baseUrl + "/withPagination"
let ignoreUrl = baseUrl + "/ignore"

type SettlementsResponse = JsonProvider< """
    {
        "count": 1,
        "items": [ {
            "id": "111e2d75-c86b-f237-871e-54c132d90f54",
            "contractEndDate": "2021-01-26"
        } ]
    }
""" >

let getIdsToIgnore (response: SettlementsResponse.Root) =
    response.Items
    |> Array.filter (fun s -> s.ContractEndDate.Year < 2022)
    |> Array.map (fun s -> s.Id)

let idsToIgnore =
    http {
        GET withoutPagingUrl
    }
    |> (fun c -> c.content.ReadAsStringAsync().Result)
    |> SettlementsResponse.Parse
    |> getIdsToIgnore

printfn "%i settlements to ignore" idsToIgnore.Length

http {
    POST ignoreUrl
    body
    json (JsonConvert.SerializeObject(idsToIgnore))
}

printfn "Settlements ignored"