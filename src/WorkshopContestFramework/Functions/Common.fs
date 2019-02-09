module Common

    open Microsoft.WindowsAzure.Storage.Table

    type User(email: string, password: string)  =
        inherit TableEntity("workshop", password)
        new() = User(null, null)
        member val Email = email with get, set
        member val Score = 0 with get, set

