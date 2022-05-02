#r "nuget: FsHttp"
#r "nuget: FSharp.Data"
#r "nuget: Newtonsoft.Json"

open FsHttp
open FSharp.Data
open Newtonsoft.Json

type SettlementsResponse = JsonProvider<
    """
    {
        "count": 1,
        "items": [
            {
                "id": "c4df8d6b-be6b-48db-9bfc-32b2872683ad",
                "contractEndDate": "2014-07-21T00:00:00"
            }
        ]
    }""" >

let limit = 40

let settlementsAtPage pageNumber =
    let parseResult response =
        response.content.ReadAsStringAsync().Result
        |> SettlementsResponse.Parse

    http {
        GET "http://localhost:8085/settlements/withPagination"
        query [
            "limit", string limit
            "page", string pageNumber
        ]
    }
    |> Request.send
    |> parseResult
    
let idsToIgnore =
    seq {
        let settlementsPage1 = settlementsAtPage 1
        yield settlementsPage1

        let totalPages = (settlementsPage1.Count / limit) + 1
        if totalPages > 1 then
            yield! 
                [2..totalPages]
                |> Seq.map settlementsAtPage
    }
    |> Seq.collect (fun p -> p.Items)
    |> Seq.takeWhile (fun s -> s.ContractEndDate.Year < 2022)
    |> Seq.map (fun s -> s.Id)
    |> Seq.toList

printfn "%i ids to ignore" idsToIgnore.Length

http {
    POST "http://localhost:8085/settlements/ignore"
    body
    json (JsonConvert.SerializeObject(idsToIgnore))
}
|> Request.send
|> ignore

printfn "Settlements ignored successfully"