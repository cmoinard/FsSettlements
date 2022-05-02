namespace FsSettlements.Web

open System
open Saturn
open Giraffe
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Builder

type User = {
    Id: uint
    FirstName: string
    LastName: string
}

type Settlement = {
    Id: Guid
    User: User
    ContractEndDate: DateTime
}

type SettlementsResponse = {
    Items: Settlement list
    Count: int
}

[<CLIMutable>]
type PageQuery = {
    Limit: int
    Page: int
}

module Program =
    let settlementFaker =
        let lowerDate = DateTime(2013, 01, 01)
        let higherDate = DateTime(2022, 06, 30)
        
        Bogus.Faker<Settlement>()
            .CustomInstantiator(fun f -> 
                { Id = f.Random.Guid ()
                  ContractEndDate = f.Date.Between(lowerDate, higherDate)
                  User = {
                      Id = f.Random.UInt ()
                      FirstName = f.Name.FirstName ()
                      LastName = f.Name.LastName ()
                  }
                })
            
    let mutable settlements =
        let generatedSettlements = settlementFaker.Generate(820)
        
        {
            Items = List.ofSeq generatedSettlements
            Count = generatedSettlements.Count
        }
        
    let getPagedSettlementsHandler =
        let getPage query =
            let nbItemsToSkip =
                Math.Min(
                    settlements.Items.Length,
                    (query.Page - 1) * query.Limit)
            
            let nbItemsToTake =
                Math.Min(
                    query.Limit,
                    settlements.Items.Length - nbItemsToSkip)
            
            {
                Count = settlements.Count
                Items =
                    settlements.Items
                    |> List.skip nbItemsToSkip
                    |> List.take nbItemsToTake
            }            
            |> json
            
        let getDefaultPage = getPage { Limit = 40; Page = 1 } 
        tryBindQuery<PageQuery> (fun _ -> getDefaultPage) None getPage
        
    let ignoreIds =
        bindJson<Guid list> (fun ids ->
            let settlementsToIgnore =
                settlements.Items
                |> List.filter (fun s ->
                    ids |> List.exists ((=) s.Id))
                
            let settlementsAfterIgnore =
                settlements.Items |> List.except settlementsToIgnore
                
            settlements <-
                {
                    Items = settlementsAfterIgnore
                    Count = settlementsAfterIgnore.Length
                }
            
            String.Join("\n", ids)
            |> text)
        
    let settlementsRoute =
        router {
            get "/pending" (json settlements)
            get "/withPaging" (warbler (fun _ -> getPagedSettlementsHandler))
            post "/ignore" (warbler (fun _ -> ignoreIds))
        }
        
    let mainRoute =
        router {
            forward "/settlements" settlementsRoute
        }
        
    let app =
        application {
            use_router mainRoute
            url "http://localhost:8085/"
            memory_cache
            use_static "static"
            use_gzip
            
            app_config (fun app ->
              let env = Environment.getWebHostEnvironment app
              if (env.IsDevelopment()) then
                  app.UseDeveloperExceptionPage()
              else
                app
            )
        }

    [<EntryPoint>]
    let main _ =
        run app

        0 // Exit code