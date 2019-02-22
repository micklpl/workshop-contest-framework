# Workshop Contest Framework
A generic framework that allows you to quickly setup a knowledge contest for your technical workshop's participants. Written in F#, runs on Azure Functions, stores data in Azure Storage (Tables and Blobs).

![](https://i.imgur.com/HoO7WGy.png)


# how it works ?
- deploy the application on your Resource Group in Azure
- upload `metadata.json` file into `content` container in your blob storage (following template published in this repo)
- modify html templates from `content` container in order to add your company logo / other 
- using `Initialize` functions administrator can provide a list of contest participants (their emails)
- using `Start` function administrator triggers email delivery (via SendGrid), each participant receives an email with a link to contest's main page, such link includes his/her authentication token
- contest begins, users can submit their answers to subsequent challenges
- challenge answered correctly unlocks next challenge (if defined)
- administrator can run `Summary` function to render scoreboard that autmatically refreshes itself

# scoring rules
- every question can be answered only once by each participant
- every participant has `5` credits initially
- every wrong answer will take `1` credit
- every used hint takes `1` credit
- participant who answers a challenge first gets `4` credits
- participant who answers a challenge second gets `3` credits
- participant who answers a challenge second gets `2` credits
- every other participant who provides correct answer gets `1` credit

# setup
To run Workshop Contest Framework you need to perform following steps:
- create a **Resource Group**
- craete **Functions App**
- create **Storage Account**
- create **Send Grid** choosing free subscription

Having those resources you need to insert following keys to your Function App **Application Settings**:
- **AzureWebJobsStorage** - connection string from created Storage Account (e.g. DefaultEndpointsProtocol=https;AccountName={storagename};AccountKey={key} )
- **BaseUrl** - base url to all functions (e.g. https://wcf-dev.azurewebsites.net/api/)
- **ContestMetadata** - URL to a metadata file (can be a publicly available blob or a blob with SAS token like https://wcfsdevtorage.blob.core.windows.net/content/metadata.json?st=2019-02-09T12%3A35%3A00Z&se=2029-02-10T12%3A35%3A00Z&sp=r&sv=2018-03-28&sr=c&sig={signaturehere})
- **SendGridAccessKey** - access key generated in SendGrid portal (e.g. SG.gP_{somethinghere}-OzeR-sSA.{somethinghere})
- **SlidesUrl** - a URL where presentation slides (pptx) are uploaded, can be a blob like for ContestMetadata
- **StorageConnectionString** - connection string from created Storage Account (e.g. DefaultEndpointsProtocol=https;AccountName={storagename};AccountKey={key} )

**metadata.json** is the file where challenges and hints are defined, template can be found in source code under `src/Content/metadata.json` path

Azure Blob Storage should contain **Common** container with following files (templates can be found in source code under `src/Content` path):
- correct-answer.html
- hint.html
- incorrect-answer.html
- main.html
- puzzle.html
- results.html

Azure Table Storage tables will be created by `Initialize` function

Source code can be deployed using Azure DevOps or from Visual Studio using **Publish..** option.
