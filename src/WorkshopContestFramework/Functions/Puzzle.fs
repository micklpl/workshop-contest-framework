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

module Puzzle =
    [<FunctionName("Puzzle")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Anonymous, [|"get"|])>] req: HttpRequest) (log: ILogger) = 
        async {
            let authenticationKey = req.Query.["key"] |> Seq.head
            let riddleId = req.Query.["id"] |> Seq.head

            let contestMetadata = Environment.GetEnvironmentVariable "ContestMetadata" |> Metadata.Load

            let! user = getUserByKey authenticationKey
            let! content = getHtml ("common", "puzzle.html")
            let! challenge = getChallengeByKey riddleId

            let challengeContent = contestMetadata.Challenges |> Seq.find(fun c -> c.Order = challenge.Level)
            
            let compiledContent = content.Replace("{{ title }}", challengeContent.Title)
                                         .Replace("{{ body }}", challengeContent.Body)
           
            let response = new ContentResult();
            response.Content <- compiledContent;
            response.ContentType <- "text/html";
            return response;
        }
        |> Async.StartAsTask