#r "nuget: FsHttp"
#r "nuget: Newtonsoft.Json"

open FsHttp
open FsHttp.DslCE
open FSharp.Data
open Newtonsoft.Json

let limit = 40
let baseUrl = "http://localhost:8085"
let pendingUrl = baseUrl + "/withPagination"
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
    let getPage (page: int) =
        http {
            GET pendingUrl
            query [
                "limit", string limit
                "page", string page
            ]
        }
        |> (fun c -> c.content.ReadAsStringAsync().Result)
        |> SettlementsResponse.Parse

    seq {
        let page1 = getPage 1
        yield page1

        let pagesCount = 1 + (page1.Count / limit)
        if pagesCount > 1 then
            yield!
                [2..pagesCount]
                |> Seq.map getPage
        
    }
    |> Seq.collect (fun p -> p.Items)
    |> Seq.filter (fun s -> s.ContractEndDate.Year < 2022)
    |> Seq.map (fun s -> s.Id)
    |> Seq.toList

printfn "%i settlements to ignore" idsToIgnore.Length

http {
    POST ignoreUrl
    body
    json (JsonConvert.SerializeObject(idsToIgnore))
}

printfn "Settlements ignored"