using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using FSharpHandlers;
using MediatR;
using WebApplication1.Handlers;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
      

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IMediator _mediator;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IEnumerable<CSharpWeatherForecast>> Get()
        {
            return await _mediator.Send(new GetWeatherRequest());
        }
        
        [HttpGet("fsharp")]
        public async Task<IEnumerable<Domain.WeatherForecast>> GetFSharp()
        {
            return await _mediator.Send(new FunctionalHandlers.FSharpGetWeatherForecastRequest());
        }
    }
}
