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

module Results =
    [<FunctionName("Results")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Function, [|"get"|])>] req: HttpRequest) (log: ILogger) = 
        async {
            let storageAccount = createStorageAccount()
            let tableClient = storageAccount.CreateCloudTableClient ()
            let challenges = tableClient.GetTableReference "users"
            let usersQuery =
                TableQuery<User>().Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey", QueryComparisons.Equal, "workshop"))

            let! users = challenges.ExecuteQuerySegmentedAsync(usersQuery, null) |> Async.AwaitTask
            
            let usersTable = users |> Seq.sortByDescending(fun s -> s.Score)
                                   |> Seq.mapi(fun i el -> String.Format("<tr><th scope=\"row\">{0}</th><td>{1}</td><td>{2}</td></tr>", i + 1, el.Email, el.Score))

            let! results = getHtml("common", "results.html")

            let content = results.Replace("{{ users }}", usersTable |> String.concat "")

            let response = new ContentResult()
            response.Content <- content
            response.ContentType <- "text/html"

            return response
        }
        |> Async.StartAsTask