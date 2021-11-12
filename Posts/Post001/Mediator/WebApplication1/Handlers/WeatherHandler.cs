using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSharpHandlers;
using MediatR;

namespace WebApplication1.Handlers
{
    public record GetWeatherRequest : IRequest<IEnumerable<CSharpWeatherForecast>>;
    
    
    public class WeatherHandler : IRequestHandler<GetWeatherRequest, IEnumerable<CSharpWeatherForecast>>
    {
        private readonly Domain.IDescriptionProvider _descriptionProvider;

        public WeatherHandler(Domain.IDescriptionProvider descriptionProvider)
        {
            _descriptionProvider = descriptionProvider;
        }
       
        public Task<IEnumerable<CSharpWeatherForecast>> Handle(GetWeatherRequest request, CancellationToken cancellationToken)
        {
            var rng = new Random();

            var summaries = _descriptionProvider.Get().ToArray();
            IEnumerable<CSharpWeatherForecast> result = Enumerable.Range(1, 5).Select(index => new CSharpWeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = summaries[rng.Next(summaries.Length)]
                })
                .ToArray();

            return Task.FromResult(result);
        }
    }
}