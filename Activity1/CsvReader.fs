module CsvReader

open System

type Status = Unwrapped | Wrapped | Rewrappable | Ok | Invalid
type State = {
    records: string[] list
    record: string list
    field: char list
    status: Status
    inputs: char list
}
with
    static member Default = {
        records = []
        record = []
        field = []
        status = Unwrapped
        inputs = List.empty
    }

let pushInput field c =
    c :: field

let commitField field =
    field |> List.rev |> Array.ofList |> String

let pushField fields field =
    field :: fields

let commitRecord record =
    record |> List.rev |> Array.ofList

let pushRecord records record =
    record :: records

let commitRecords records =
    records |> List.rev

let finalizeRecords state =
    let { records = records; record = record; field = field } = state

    // Handle optional CRLF ending of the last record
    let records =
        if not (List.isEmpty field && List.isEmpty record) then
            field
            |> commitField
            |> pushField record
            |> commitRecord
            |> pushRecord records
        else records
    commitRecords records

let processLf inputs =
    match inputs with
    | '\n' :: t -> Some(t)
    | _ -> None

let unwrappedTransition state =
    let { records = records; record = record; field = field; inputs = inputs } = state
    match inputs with
    | '"' :: remainingInputs ->
        { state with
            status = Wrapped
            inputs = remainingInputs
        }
    | ',' :: remainingInputs ->
        let newRecord =
            field
            |> commitField
            |> pushField record

        { state with
            record = newRecord
            field = []
            status = Unwrapped
            inputs = remainingInputs
        }
    | '\r' :: remainingInputs ->
        match processLf remainingInputs with
        | Some(remainingInputs) ->
            let newRecords =
                field
                |> commitField
                |> pushField record
                |> commitRecord
                |> pushRecord records

            { state with
                records = newRecords
                record = []
                field = []
                status = Unwrapped
                inputs = remainingInputs
            }
        | None -> { state with status = Invalid; inputs = remainingInputs }
    | '\n' :: remainingInputs ->
        { state with status = Invalid; inputs = remainingInputs }
    | c :: remainingInputs ->
        let newField = pushInput field c
        { state with
            field = newField
            status = Unwrapped
            inputs = remainingInputs
        }
    // EOF
    | [] ->
        let newRecords = finalizeRecords state
        { state with
            records = newRecords
            record = []
            field = []
            status = Ok
        }

let wrappedTransition state =
    let { field = field; inputs = inputs } = state
    match inputs with
    | '"' :: remainingInputs ->
        { state with
            status = Rewrappable
            inputs = remainingInputs
        }
    | c :: remainingInputs ->
        let newField = pushInput field c
        { state with
            field = newField
            status = Wrapped
            inputs = remainingInputs
        }
    // EOF
    | [] ->
        { state with
            status = Invalid
            inputs = []
        }

let rewrappableTransition state =
    let { records = records; record = record; field = field; inputs = inputs } = state
    match inputs with
    | '\r' :: remainingInputs ->
        match processLf remainingInputs with
        | Some(remainingInputs) ->
            let newRecords =
                field
                |> commitField
                |> pushField record
                |> commitRecord
                |> pushRecord records

            { state with
                records = newRecords
                record = []
                field = []
                status = Unwrapped
                inputs = remainingInputs
            }
        | None -> { state with status = Invalid; inputs = remainingInputs }
    | ',' :: remainingInputs ->
        let newRecord =
            field
            |> commitField
            |> pushField record

        { state with
            record = newRecord
            field = []
            status = Unwrapped
            inputs = remainingInputs
        }
    | '"' :: remainingInputs ->
        let newField = pushInput field '"'
        { state with
            field = newField
            status = Wrapped
            inputs = remainingInputs
        }
    | c :: remainingInputs ->
        { state with
            status = Invalid
            inputs = remainingInputs
        }
    // EOF
    | [] ->
        let newRecords = finalizeRecords state
        { state with
            records = newRecords
            record = []
            field = []
            status = Ok
        }

let invalidTransition state =
    { state with inputs = List.empty }

let okTransition state =
    { state with status = Invalid; inputs = List.empty }

let transitionFunction status =
    match status with
    | Unwrapped -> unwrappedTransition
    | Wrapped -> wrappedTransition
    | Rewrappable -> rewrappableTransition
    | Ok -> okTransition
    | Invalid -> invalidTransition

let rec validateAndParse state =
    match state.status with
    | Ok | Invalid ->
        let records = state.records
        if List.isEmpty records then state
        else
            let count = records |> List.head |> Array.length
            let rule4 = records |> List.map Array.length |> List.forall ((=) count)
            if rule4 then state
            else { state with status = Invalid }
    | status ->
        let transition = transitionFunction status
        state |> transition |> validateAndParse
