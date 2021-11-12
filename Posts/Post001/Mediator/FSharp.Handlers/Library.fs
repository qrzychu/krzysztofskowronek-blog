namespace FSharpHandlers

open System
open System.Collections.Generic
open System.Threading.Tasks
open MediatR

module FunctionalHandlers =
    open Domain
        
    type FSharpGetWeatherForecastRequest() =
        interface IRequest<IEnumerable<WeatherForecast>>
    
    type GetWeatherForecastHandler(descriptionProvider : IDescriptionProvider) =        
        interface IRequestHandler<FSharpGetWeatherForecastRequest, IEnumerable<WeatherForecast>> with
            member this.Handle(request, cancellationToken) =
                let rnd = Random()
                
                let summaries = descriptionProvider.Get() |> Seq.toArray
                
                [1..5] |> Seq.map (fun _ -> {
                    Date = DateTime.UtcNow
                    TemperatureC = rnd.Next(-20, 55) * 1<C>
                    Summary = summaries.[rnd.Next summaries.Length]
                })
                |> Task.FromResult
            
                
       
    