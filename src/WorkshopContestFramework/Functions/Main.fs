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
open Microsoft.WindowsAzure.Storage.Table
open Newtonsoft.Json
open Common

module Main =
    [<FunctionName("Main")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Anonymous, [|"get"|])>] req: HttpRequest) (log: ILogger) = 
        async {
            let authenticationKey = req.Query.["key"] |> Seq.head
            "Received request for " + authenticationKey |> log.LogInformation

            let connectionString = Environment.GetEnvironmentVariable "StorageConnectionString"

            let storageAccount = CloudStorageAccount.Parse connectionString
            let tableClient = storageAccount.CreateCloudTableClient ()
            let users = tableClient.GetTableReference "users" 

            let retrieveOperation = TableOperation.Retrieve<User>("workshop", authenticationKey);
            let! retrievedResult = users.ExecuteAsync retrieveOperation |> Async.AwaitTask
                                 
            let user = retrievedResult.Result :?> User

            return user.Email
        }
        |> Async.StartAsTask