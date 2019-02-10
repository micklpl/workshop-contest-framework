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

            let! user = getUserByKey authenticationKey
            let! content = getHtml ("common", "main.html")
            
            // challenges
            let storageAccount = createStorageAccount()
            let tableClient = storageAccount.CreateCloudTableClient ()
            let challenges = tableClient.GetTableReference "challenges"
            let query =
                TableQuery<Challenge>().Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey", QueryComparisons.Equal, "workshop"))

            let! allChallenges = challenges.ExecuteQuerySegmentedAsync(query, null) |> Async.AwaitTask

            let baseUrl = Environment.GetEnvironmentVariable "BaseUrl"
            let userChallenges = allChallenges.Results
                                    |> Seq.filter(fun c -> c.Level <= user.Level)
                                    |> Seq.sortBy(fun c -> c.Level)
                                    |> Seq.map(fun c -> String.Format("<a href=\"{1}Puzzle?id={2}&key={3}\" class=\"list-group-item list-group-item-action\">{0}</a>", c.Title, baseUrl, c.RowKey, authenticationKey))

            let compiledContent = content.Replace("{{ email }}", user.Email)
                                         .Replace("{{ score }}", user.Score.ToString())
                                         .Replace("{{ challenges }}", userChallenges |> String.concat "")
           
            let response = new ContentResult();
            response.Content <- compiledContent;
            response.ContentType <- "text/html";
            return response;
        }
        |> Async.StartAsTask