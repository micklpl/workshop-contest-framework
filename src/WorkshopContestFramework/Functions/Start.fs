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
open SendGrid
open SendGrid.Helpers.Mail
open System.Collections.Generic

module Start =
    let sendEmail (user:User) =
        async {
            let key = Environment.GetEnvironmentVariable "SendGridAccessKey"
            let baseUrl = Environment.GetEnvironmentVariable "BaseUrl"
            let msg = new SendGridMessage()
            msg.SetFrom(new EmailAddress("contest@example.com", "F# Workshop Contest"))

            let recipients = new List<EmailAddress>()
            recipients.Add(new EmailAddress(user.Email, user.Email.Split('@').[0]))
            msg.AddTos(recipients);

            msg.SetSubject("Welcome to F# Workshop Contest");    
            let body = String.Format("<p>Hello!</p><br/><p>To join Workshop Contest click <a href={0}Main?key={1}>here</a>", baseUrl, user.RowKey);

            msg.AddContent(MimeType.Html, body);
    
            let client = new SendGridClient(key);

            let! response = client.SendEmailAsync(msg) |> Async.AwaitTask
            return response
        }

    [<FunctionName("Start")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Function, [|"post"|])>] req: HttpRequest) (log: ILogger) = 
        async {
            let! users = getAllUsers()
            let tasks = users |> Seq.map(fun u -> sendEmail(u))
                              |> Async.Parallel
                              |> Async.RunSynchronously 

            return "done"
        }
        |> Async.StartAsTask