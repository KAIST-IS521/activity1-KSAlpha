# IS521-2019S Activity 1

The goal of this activity is to create a program which is available to validate a CSV file and to read fields of the specific column of the well-formed (valid) CSV file.

So, the problem is consisted of three parts:

1. Validating a CSV file
2. Extracting fields of the specific column from records
3. Printing extracted fields



## Algorithm

### Validating a CSV file

This project suggests Finite State Machine (FSM) with three extra rules to solve this problem.

Based on a [RFC4180][RFC4180] given in the activity readme file of this course, I created a simple FSM to validate the syntax of a CSV file.

![Finite State Machine validating a CSV file](fsm_csv_syntax_validation.svg)

Unfortunately, this model is not able to express rule 2, 3 and 4 of [RFC4180][RFC4180]. Three extra rules are added to address this:

1. Even if `EOF` is reached without preceding `CRLF`, the algorithm will assume that `CRLF` exists at the end of the file.

   This handles rule 2 (i.e., optional `CRLF` ending of the last record).

2. Optional header record will be ignored as it doesn't affect the output of the program (Also, there is no deterministic way to recover an intended MIME type using only a file).

   This resolves rule 3 (i.e., optional header record recognized by MIME type definition in the beginning of the file).

3. The number of fields in each record will be counted and compared after the FSM algorithm terminates with `Ok` state.

   This addresses rule 4 (i.e., one or more fields in each record and equal number of fields in each record).

`Ok` state continues the algorithm and `Err` state exits the program with status code 1.

WIP: Implement algorithm



### Extracting fields of the specific column from records

Fields and records are parsed using the output of the FSM of the previous step (i.e., CSV validation). Parse result is expected to have a type of `(string list) list`, which inner lists are lists of fields and outer list is a list of records. This process is expected to be merged in to the algorithm of the previous step.

Wanted fields are extracted by mapping inner lists into fields, which is achieved by selecting the entry using the specific index of the list. In this process, `ListOutOfBounds` exception may occur due to bad user inputs.

Any exceptions results in the program to exit with status code 1. Otherwise, the algorithm continues.



### Printing extracted fields

The program creates a string by joining extracted fields with desired line break (i.e., `LF` in Unix, `CRLF` in Windows) and writes out the string to `stdout`. Unexpected low-level I/O exceptions during write is not handled in the algorithm.



## Possible improvements

- Better memory management

  The naïve approach in memory management of this algorithm may cause an out-of-memory (OOM) exception when handling extremely large files.

- WIP



## Running the program

WIP



[RFC4180]: https://tools.ietf.org/html/rfc4180