﻿module Common

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
    open FSharp.Data

    type User(email: string, password: string)  =
        inherit TableEntity("workshop", password)
        new() = User(null, null)
        member val Email = email with get, set
        member val Score = 5 with get, set
        member val Level = 1 with get, set

    type Challenge(level: int, code: string, answer: string, title: string)  =
        inherit TableEntity("workshop", code)
        new() = Challenge(0, null, null, null)
        member val Level = level with get, set
        member val Title = title with get, set
        member val Answer = answer with get, set

    type Answer(challenge: string, providedAnswer: string, answerId: string, isCorrect: bool, userId: string)  =
        inherit TableEntity(challenge, answerId)
        new() = Answer(null, null, null, false, null)
        member val ProvidedAnswer = providedAnswer with get, set
        member val IsCorrect = isCorrect with get, set
        member val UserId = userId with get, set

    type Metadata = JsonProvider<"https://wcfsdevtorage.blob.core.windows.net/common/metadata-template.json?st=2019-02-09T14%3A24%3A04Z&se=2029-02-10T14%3A24%3A00Z&sp=r&sv=2018-03-28&sr=b&sig=EdoP7Yhe0AP3AqkjsIMGJSqI2SwKxwBMz8B6EHWqPqI%3D">

    let randomStr = 
        let chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
        let charsLen = chars.Length
        let random = System.Random()

        fun len -> 
            let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
            new System.String(randomChars)

    let createStorageAccount () =
        let connectionString = Environment.GetEnvironmentVariable "StorageConnectionString"
        CloudStorageAccount.Parse connectionString

    let retrieveEntity<'T when 'T :> ITableEntity> (tableName, key) =
        async {
            let storageAccount = createStorageAccount()
            let tableClient = storageAccount.CreateCloudTableClient ()
            let table = tableClient.GetTableReference tableName 

            let retrieveOperation = TableOperation.Retrieve<'T>("workshop", key);
            let! retrievedResult = table.ExecuteAsync retrieveOperation |> Async.AwaitTask

            return retrievedResult
        }

    let replaceEntity<'T when 'T :> ITableEntity> (tableName, entity) =
        async {
            let storageAccount = createStorageAccount()
            let tableClient = storageAccount.CreateCloudTableClient ()
            let table = tableClient.GetTableReference tableName 

            let updateOperation =  TableOperation.Replace(entity);
            let! updateResult = table.ExecuteAsync updateOperation |> Async.AwaitTask

            return updateResult
        }

    let getUserByKey key =
        async {
            let! retrievedResult = retrieveEntity<User>("users", key)                                 
            let user = retrievedResult.Result :?> User
            return user
        }

    let getChallengeByKey key =
        async {
            let! retrievedResult = retrieveEntity<Challenge>("challenges", key)                                 
            let user = retrievedResult.Result :?> Challenge
            return user
        }

    let getHtml (container, filename) =
        async {
            let storageAccount = createStorageAccount()
            let cloudBlobClient = storageAccount.CreateCloudBlobClient()
            let common = cloudBlobClient.GetContainerReference container
            let blob = common.GetBlobReference filename

            use memoryStream = new MemoryStream()
            do! blob.DownloadToStreamAsync memoryStream |> Async.AwaitTask
            let bytes = memoryStream.ToArray()
            let content = System.Text.Encoding.UTF8.GetString(bytes)

            return content
        }

    let saveEntity<'T when 'T :> ITableEntity> (tableName, entity) =
        async {
            let storageAccount = createStorageAccount()
            let tableClient = storageAccount.CreateCloudTableClient ()
            let table = tableClient.GetTableReference tableName 

            let insert = TableOperation.Insert entity
            
            let! result = table.ExecuteAsync insert |> Async.AwaitTask

            return ()
        }

    let getAllUsers() =
        async{
            let storageAccount = createStorageAccount()
            let tableClient = storageAccount.CreateCloudTableClient ()
            let challenges = tableClient.GetTableReference "users"
            let usersQuery =
                TableQuery<User>().Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey", QueryComparisons.Equal, "workshop"))

            let! users = challenges.ExecuteQuerySegmentedAsync(usersQuery, null) |> Async.AwaitTask
            return users
        }
