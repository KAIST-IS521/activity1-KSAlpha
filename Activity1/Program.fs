open System
open System.IO

open CsvReader

[<EntryPoint>]
let main argv =
    if Array.length argv <> 2 then
        eprintfn "Not enough arguments specified"
        1
    else
        let filename = argv.[0]
        let fileContents =
            filename
            |> File.ReadAllText
            |> Seq.map (fun c -> Some(c))
            |> List.ofSeq

        let initialState = {
            State.Default with
                input = fileContents @ [ None ]
        }
        let { records=records; status=status } = validateAndParse initialState
        printfn "%A\n%A" records status
        0
