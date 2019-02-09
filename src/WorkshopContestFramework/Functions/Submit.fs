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

            let storageAccount = createStorageAccount()
            let tableClient = storageAccount.CreateCloudTableClient ()
            let challenges = tableClient.GetTableReference "answers"
            let query =
                TableQuery<Answer>().Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey", QueryComparisons.Equal, riddleId))

            let! answers = challenges.ExecuteQuerySegmentedAsync(query, null) |> Async.AwaitTask

            let correctAnswers = answers |> Seq.filter(fun s -> s.IsCorrect = true) 
                                         |> Seq.map(fun s -> s.UserId)
                                         |> List.ofSeq

            let score = match (isCorrect, correctAnswers) with
                            | (false, _) -> -1
                            | (true, x) when x |> List.exists (fun elem -> elem = authenticationKey) -> 0
                            | (true, [ ] ) -> 4
                            | (true, [ _ ] ) -> 3
                            | (true, [ _ ; _ ] ) -> 2
                            | ( _, _ ) -> 1

            user.Score <- user.Score + score
            let! result = replaceEntity("users", user)   
            
            let answer = new Answer(riddleId, answer, randomStr 10, isCorrect, authenticationKey)
            do! saveEntity("answers", answer)

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