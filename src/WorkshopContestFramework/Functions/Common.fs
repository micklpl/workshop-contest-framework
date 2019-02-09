module Common

    open Microsoft.WindowsAzure.Storage.Table

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
