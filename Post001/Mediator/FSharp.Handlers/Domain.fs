namespace FSharpHandlers

open Microsoft.Extensions.Logging

module Domain =
    open System

    /// <summary>
    /// Celsius degrees for temperature
    /// </summary>
    [<Measure>]
    type C
    
    /// <summary>
    /// Fahrenheit degrees for temperature
    /// </summary>
    [<Measure>] // Units of measure
    type F
       
    type WeatherForecast = {
        Date : DateTime
        TemperatureC: int<C>
        Summary : string
    }
    
    let getTemperatureInF (t : int<C>) =
        (float t) / 0.5556
        |> (+) 32.0
        |> int
        |> (*) 1<F>
        
    type IDescriptionProvider =
        abstract member Get : unit -> string seq
        
    type DescriptionProvider (log : ILogger<DescriptionProvider>) =
        interface IDescriptionProvider with
            member this.Get() =
                log.LogInformation "Hello from F#!"
                [|"Freezing"; "Bracing"; "Chilly"; "Cool"; "Mild"; "Warm"; "Balmy"; "Hot"; "Sweltering"; "Scorching"|]
                |> Array.toSeq