open System
open System.IO

open CsvReader

let fail msg =
    eprintfn "%s" msg
    exit 1

let intOrFail (i: String) =
    match Int32.TryParse i with
    | true, i -> i
    | false, _ -> fail "Cannot parse string into integer"

[<EntryPoint>]
let main argv =
    if Array.length argv <> 2 then
        fail "Unexpected argument count"

    let columnIndex = intOrFail argv.[1] - 1
    let filename = argv.[0]
    let fileContents =
        filename
        |> File.ReadAllText
        |> List.ofSeq

    let initialState = { State.Default with inputs = fileContents }
    let { records=records; status=status } = validateAndParse initialState
    if status <> Ok then
        fail "Invalid CSV format"

    let wantedFields =
        try records |> List.map (Array.item columnIndex)
        with
        | :? IndexOutOfRangeException -> fail "Column index out of range"
        | _ -> fail "Unknown exception occurred"

    let outputString = String.Join (Environment.NewLine, wantedFields)
    printfn "%s" outputString
    0
