module CsvReader

open System

type Status = Unwrapped | Wrapped | Rewrappable | Ok | Invalid
type State = {
    records: string list list
    record: string list
    field: char list
    status: Status
    input: char option list
}
with
    static member Default = {
        records = []
        record = []
        field = []
        status = Unwrapped
        input = List.empty
    }

let pushInput field c =
    c::field

let commitField field =
    field |> List.rev |> Array.ofList |> String

let pushField fields field =
    field::fields

let commitRecord record =
    record |> List.rev

let pushRecord records record =
    record::records

let commitRecords records =
    records |> List.rev

let finalizeRecords state =
    let { records=records; record=record; field=field } = state

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
    let nextHead = List.tryHead inputs
    match nextHead with
    | Some(Some('\n')) -> Some(List.tail inputs)
    | _ -> None

let unwrappedTransition state =
    let { records=records; record=record; field=field; input=input } = state
    let inputHead = List.head input
    let nextInput = List.tail input
    match inputHead with
    | Some('\r') ->
        match processLf nextInput with
        | Some(nextInput) ->
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
                input = nextInput
            }
        | None -> { state with status = Invalid; input = nextInput }
    | Some('\n') ->
        { state with status = Invalid; input = nextInput }
    | Some(',') ->
        let newRecord =
            field
            |> commitField
            |> pushField record

        { state with
            record = newRecord
            field = []
            status = Unwrapped
            input = nextInput
        }
    | Some('"') ->
        { state with
            status = Wrapped
            input = nextInput
        }
    | Some(c) ->
        let newField = pushInput field c
        { state with
            field = newField
            status = Unwrapped
            input = nextInput
        }
    | None ->
        let newRecords = finalizeRecords state
        { state with
            records = newRecords
            record = []
            field = []
            status = Ok
            input = nextInput
        }

let wrappedTransition state =
    let { field=field; input=input } = state
    let inputHead = List.head input
    let nextInput = List.tail input
    match inputHead with
    | Some('"') ->
        { state with
            status = Rewrappable
            input = nextInput
        }
    | Some(c) ->
        let newField = pushInput field c
        { state with
            field = newField
            status = Wrapped
            input = nextInput
        }
    | None ->
        { state with
            status = Invalid
            input = nextInput
        }

let rewrappableTransition state =
    let { records=records; record=record; field=field; input=input } = state
    let inputHead = List.head input
    let nextInput = List.tail input
    match inputHead with
    | Some('\r') ->
        match processLf nextInput with
        | Some(nextInput) ->
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
                input = nextInput
            }
        | None -> { state with status = Invalid; input = nextInput }
    | Some(',') ->
        let newRecord =
            field
            |> commitField
            |> pushField record

        { state with
            record = newRecord
            field = []
            status = Unwrapped
            input = nextInput
        }
    | Some('"') ->
        let newField = pushInput field '"'
        { state with
            field = newField
            status = Wrapped
            input = nextInput
        }
    | Some(c) ->
        { state with
            status = Invalid
            input = nextInput
        }
    | None ->
        let newRecords = finalizeRecords state
        { state with
            records = newRecords
            record = []
            field = []
            status = Ok
            input = nextInput
        }

let invalidTransition state =
    { state with input = List.empty }

let okTransition state =
    { state with status = Invalid; input = List.empty }

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
            let count = records |> List.head |> List.length
            let rule4 = records |> List.map List.length |> List.forall ((=) count)
            if rule4 then state
            else { state with status = Invalid }
    | status ->
        let transition = transitionFunction status
        state |> transition |> validateAndParse
