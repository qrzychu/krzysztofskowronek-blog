namespace FSharpSamples

open System

module Domain =
    // in reality this would be a separate file in bigger project
    type User = { Username: string; Id: int }

    type SomeErrorType =
        | UserNotFound of int
        | ConnectionIssue

module UserHelpers =
    // in reality this would be a separate file in bigger project
    open Domain

    let tryGetUser (id: int) : User option = Some { Username = "SomeUser"; Id = id }

    // F# has powerful type inference, no for type annotations
    let lowerUsername user = user.Username.ToLower()

    let userOrError (id: int) : Result<string, SomeErrorType> =
        match tryGetUser id |> Option.map lowerUsername with // if tryGetUser returns None, this also returns None
        | Some username -> Ok username
        | None -> Error(UserNotFound id)

module App =
    // in reality this would be a separate file in bigger project
    open UserHelpers
    open Domain

    let userId = 420

    match userId |> userOrError with
    | Ok username -> Console.WriteLine username
    | Error (UserNotFound id) -> Console.WriteLine $"User {id} not found"
    | Error ConnectionIssue -> Console.WriteLine $"Connection issue"

    let putStringBetweenTwoInts (int1: int) (str: string) (int2: int) : string =
        $"{int1} {str} {int2}"

    let f1: int -> string -> int -> string = putStringBetweenTwoInts
    let f2: string -> int -> string = f1 2
    let f3: int -> string = f2 "+"
    let f3_alt: int -> string =  putStringBetweenTwoInts 2 "+"
    let result: string = f3 3
