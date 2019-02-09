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

module Hint =
    [<FunctionName("Hint")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Anonymous, [|"get"|])>] req: HttpRequest) (log: ILogger) = 
        async {
            let authenticationKey = req.Query.["key"] |> Seq.head
            let hintId = req.Query.["id"] |> Seq.head |> int
            let challengeId = req.Query.["challengeId"] |> Seq.head |> int
            
            let contestMetadata = Environment.GetEnvironmentVariable "ContestMetadata" |> Metadata.Load

            let hint = contestMetadata.Challenges.[challengeId].Hints.[hintId]

            let! user = getUserByKey authenticationKey
            user.Score <- user.Score - 1
            let! result = replaceEntity("users", user)  

            let! hintContent = getHtml ("common", "hint.html")
            let content = hintContent.Replace("{{ question }}", hint.Question)
                                     .Replace("{{ answer }}", hint.Answer)

            let response = new ContentResult()
            response.Content <- content
            response.ContentType <- "text/html"

            return response
        }
        |> Async.StartAsTask