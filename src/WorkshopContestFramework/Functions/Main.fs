namespace FunctionApp1

open System
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open System;
open System.IO;
open System.Net;
open System.Net.Http.Headers;
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

            let cloudBlobClient = storageAccount.CreateCloudBlobClient()
            let common = cloudBlobClient.GetContainerReference "common"
            let blob = common.GetBlobReference "main.html"

            use memoryStream = new MemoryStream()
            do! blob.DownloadToStreamAsync memoryStream |> Async.AwaitTask
            let bytes = memoryStream.ToArray()
            let content = System.Text.Encoding.UTF8.GetString(bytes)

            let compiledContent = content.Replace("{{ email }}", user.Email).Replace("{{ score }}", user.Score.ToString())

            let response = new ContentResult();
            response.Content <- compiledContent;
            response.ContentType <- "text/html";
            return response;
        }
        |> Async.StartAsTask