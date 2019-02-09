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

module Submit =
    [<FunctionName("Submit")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Anonymous, [|"post"|])>] req: HttpRequest) (log: ILogger) = 
        async {
            let authenticationKey = req.Form.["key"] |> Seq.head
            let answer = req.Form.["answer"] |> Seq.head
            let riddleId = req.Form.["riddleId"] |> Seq.head
            
            let! user = getUserByKey authenticationKey
            let! content = getHtml ("common", "puzzle.html")
            let! challenge = getChallengeByKey riddleId

            let isCorrect = answer.Trim() = challenge.Answer

            let htmlFileName = if isCorrect = true
                                    then "correct-answer.html"
                                    else "incorrect-answer.html"

            let! html = getHtml("common", htmlFileName)
            let content = html.Replace("{{ key }}", authenticationKey)
                              .Replace("{{ id }}", riddleId)

            let response = new ContentResult()
            response.Content <- content
            response.ContentType <- "text/html"

            return response
        }
        |> Async.StartAsTask