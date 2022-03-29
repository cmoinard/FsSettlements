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
            
    let settlements =
        let generatedSettlements = settlementFaker.Generate(820)
        
        {
            Items = List.ofSeq generatedSettlements
            Count = generatedSettlements.Count
        }
        
    let getPagedSettlementsHandler =
        let getPage query =
            {
                Count = settlements.Count
                Items =
                    settlements.Items
                    |> List.skip ((query.Page - 1) * query.Limit)
                    |> List.take query.Limit
            }            
            |> json
            
        let getDefaultPage = getPage { Limit = 40; Page = 1 } 
        tryBindQuery<PageQuery> (fun _ -> getDefaultPage) None getPage
        
    let settlementsRoute =
        router {
            get "/" (json settlements)
            get "/withPagination" getPagedSettlementsHandler
        }
    let app =
        application {
            use_router settlementsRoute
            url "http://0.0.0.0:8085/"
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