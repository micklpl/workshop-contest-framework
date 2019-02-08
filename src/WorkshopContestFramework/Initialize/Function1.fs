namespace FunctionApp1

open System
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open System;
open System.IO;
open System.Threading.Tasks;
open Microsoft.AspNetCore.Mvc;
open Microsoft.Azure.WebJobs;
open Microsoft.Azure.WebJobs.Extensions.Http;
open Microsoft.AspNetCore.Http;
open Microsoft.Extensions.Logging;
open Microsoft.WindowsAzure.Storage

module Function1 =
    [<FunctionName("Initialize")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Function, [|"post"|])>] req: HttpRequest) (log: ILogger) = 
        async {
            let connectionString = Environment.GetEnvironmentVariable "StorageConnectionString"
            
            let storageAccount = CloudStorageAccount.Parse connectionString
            let tableClient = storageAccount.CreateCloudTableClient ()

            let createTable name = 
                let table = tableClient.GetTableReference name
                table.CreateIfNotExistsAsync() |> Async.AwaitTask
    
            let! result = [| "users"; "attemps"; "answers"; "permissions"; "hints" |]
                            |> Seq.ofArray
                            |> Seq.map createTable 
                            |> Async.Parallel
            
            return "done !" 
        }
        |> Async.StartAsTask