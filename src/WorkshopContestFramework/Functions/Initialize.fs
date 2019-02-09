namespace FunctionApp1

open System
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open System
open Common
open System.IO;
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging;
open Microsoft.WindowsAzure.Storage
open Newtonsoft.Json
open Microsoft.WindowsAzure.Storage.Table
open FSharp.Data

type Payload = {
   users: string[]
}

module Initialize =
    let randomStr = 
        let chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
        let charsLen = chars.Length
        let random = System.Random()

        fun len -> 
            let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
            new System.String(randomChars)

    [<FunctionName("Initialize")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Function, [|"post"|])>] req: HttpRequest) (log: ILogger) = 
        async {
            let connectionString = Environment.GetEnvironmentVariable "StorageConnectionString"
            
            let storageAccount = CloudStorageAccount.Parse connectionString
            let tableClient = storageAccount.CreateCloudTableClient ()

            //1. Create Tables

            let createTable name = 
                let table = tableClient.GetTableReference name
                table.CreateIfNotExistsAsync() |> Async.AwaitTask
    
            let! result = [| "users"; "challenges"; "answers"; "permissions"; "hints" |]
                            |> Seq.ofArray
                            |> Seq.map createTable 
                            |> Async.Parallel

            
            // 2. Create or Merge users

            let reader = new StreamReader(req.Body)
            let! payloadStr = reader.ReadToEndAsync() |> Async.AwaitTask
            let payload = JsonConvert.DeserializeObject<Payload>(payloadStr)

            let users = tableClient.GetTableReference "users"            
            let usersBatch = new TableBatchOperation()

            payload.users 
                |> Array.map(fun u -> new User(u, randomStr 16))
                |> Array.iter(fun u -> TableOperation.InsertOrMerge u |> usersBatch.Add)

            let! usersResult = users.ExecuteBatchAsync usersBatch |> Async.AwaitTask

            // 3. Initialize Content

            let metadataUrl = Environment.GetEnvironmentVariable "ContestMetadata"
            let metadata = Metadata.Load metadataUrl

            let challenges = tableClient.GetTableReference "challenges"            
            let challengesBatch = new TableBatchOperation()

            metadata.Challenges
                |> Array.map(fun c -> new Challenge(c.Order, randomStr 32, c.Answer, c.Title))
                |> Array.iter(fun u -> TableOperation.InsertOrMerge u |> challengesBatch.Add)

            let! challengesResult = challenges.ExecuteBatchAsync challengesBatch |> Async.AwaitTask
            
            return "done !"
        }
        |> Async.StartAsTask