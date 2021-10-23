# Intro

For last year or so I really got interested in functional approach to programming. I started by watching brilliant Richard Feldman talks on Elm and fell in love with the concepts. Beeing dotnet developer I discovered F#, which to my delight turned out to be Elm on steroids.

However, functional ways proved to be pretty hard to integrate into existing codebase, and I wasn't really ready to start new project using F# only. 

# "Cons" to functional programming

This may turn out to be a bit controversial, but I don't like functional ways of setting up projects. In functional world, you don't use Dependency Injection, you don't use containers and you don't use mocking for testing. 

For all of that, you use functions, partial application or IO monad. Those facts on their own are not cons - they are in most cases a pro. For starters, partial application gives you compile time checks if your dependencies are set up correctly. IO monad gives you clear, type-system level indication about functions causing side effects.

All of that is very neat, and you can even elevate your inner pride by throwing around the m-word, but in a real world, it does not compose very well with industry standards and can get very hard to manage with in bigger projects with multiple dependencies.

For example, if you want to create a REST API in F#, you would use [Giraffe](https://github.com/giraffe-fsharp/Giraffe). It's great, really. 

According to benchmarks, it's one of the most performant ways to use ASP.NET Core. 

It lowers your mental load by reducing number of concepts you need to work with - no more controllers, middleware, filters etc. Everything is just a simple, composable `IHttpHandler` - type of middleware.

I may go into detail on that in future, but for now I will redirect you to a great talk on the subject: https://www.youtube.com/watch?v=JuIq7mU50jA

What are the cons of Giraffe then? Well, Swagger? Won't work. Scoped EF Core context? Yeah, you can use it, but it feels wrong, since you literally use ASP.NET Core container to get it. 

Here you can find a sample blog application: https://github.com/ChrisPritchard/GiraffeBlog

Unfortunately, most of the "good stuff" in dotnet is made with OO in mind, designed to be consumed from C#.

# Pros of functional programing with F#

The biggest pro is conciseness - you can see much bigger part of your program on a single screen. There are few reasons for that:
- no curly braces - you save tons of lines by that
- `Bind` and `Map` functions - no more boilerplate ifs. Imagine using LINQ for the whole program, and now try to remember when was the last time you checked if the count was 0 in or before LINQ pipeline
- no `return` keyword (almost, you still use it in computation expressions)
- pattern matching

Below you will find two code blocks: one in F#, the other one in C#. They do exactly the same thing, they are "getting" a user from somewhere, and if user is found, they lowercase the username. Pretty simple.

Both represent how something like this would be achieved in a real application, given that lowering the name would be some more complex task. Remember, that in C# convention is to put every class, interface etc in a separate file. 

### C# code

```C#
using System;

namespace CSharpSamples
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
    }

    public class UserNotFoundException : Exception
    {
        public int MissingUserId { get; }

        public UserNotFoundException(int missingUserId) : base($"User {missingUserId} not found")
        {
            MissingUserId = missingUserId;
        }

        public UserNotFoundException(int missingUserId, Exception innerException)
            : base($"User {missingUserId} not found", innerException)
        {
            MissingUserId = missingUserId;
        }
    }

    public interface IUsernameLowerer
    {
        string LowerUsername(User user);
    }

    public class UsernameLowerer : IUsernameLowerer
    {
        public string LowerUsername(User user) => user.Username.ToLower();
    }

    public class UserHelper
    {
        private readonly IUsernameLowerer _usernameLowerer;

        public UserHelper(IUsernameLowerer usernameLowerer)
        {
            _usernameLowerer = usernameLowerer;
        }

        public User? TryGetUser(int id) => new User { Id = id, Username = "SomeUser" };

        public string GetUserNameLowercase(int id)
        {
            User? user = TryGetUser(id);

            if (user is null)
            {
                throw new UserNotFoundException(id);
            }

            return _usernameLowerer.LowerUsername(user);
        }
    }

    public class Program
    {
        public static void Main()
        {
            int userId = 420;

            var helper = new UserHelper(new UsernameLowerer()); // this would come from DI container

            try
            {
                var userNameLowercase = helper.GetUserNameLowercase(userId);
                Console.WriteLine(userNameLowercase);
            }
            catch (UserNotFoundException ex)
            {
                Console.WriteLine($"User {ex.MissingUserId} not found");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred: " + e.Message + Environment.NewLine + e.StackTrace);
            }
        }
    }
}
```

### F# code

```fsharp
namespace FSharpSamples

open System

module Domain =
    // in reality this would be a separate file in bigger project
    type User = { Username: string; Id: int }

    type SomeErrorType =
        | UserNotFound of int
        | ConnectionIssue

module UserHelpers =
    open Domain

    let tryGetUser (id: int) : User option = 
        Some { Username = "SomeUser"; Id = id }

    // F# has powerful type inference, no for type annotations
    let lowerUsername user = user.Username.ToLower()

    let userOrError (id: int) : Result<string, SomeErrorType> =
        match tryGetUser id |> Option.map lowerUsername with // if tryGetUser returns None, this also returns None
        | Some username -> Ok username
        | None -> Error(UserNotFound id)

module App =
    open UserHelpers
    open Domain

    let userId = 420

    match userId |> userOrError with
    | Ok username -> Console.WriteLine username
    | Error (UserNotFound id) -> Console.WriteLine $"User {id} not found"
    | Error ConnectionIssue -> Console.WriteLine $"Connection issue"
```

Few things should be apparent by now:
- Hashnode does not support syntax highlight for F# :(
- F# code takes up 38 lines and at most 74 columns
- C# code takes up 83 lines and at most 98 columns
- C# code in reality would be split into 6 files
- in F# there are no classes, just *pure* functions, meaning they depend only on their arguments
    - pure functions are super easy to test - just define arguments and expected result, assert on real result, done

I am fully aware that in both languages solution to this problem could be one or two liners, but I'm not aiming for minimal possible code. I'm aiming for "how it's done" in big project, with testability and maintainability in mind.

I would like to point out few really cool things about F# though.

### Discriminated unions and pattern matching

First feature to highlight is Discriminated Union (coming to C# soon, fingers crossed!).

```fsharp
    type SomeErrorType =
        | UserNotFound of int
        | ConnectionIssue
```

It's like an enum, but each option can be of different type (even another discriminated union) and hold information inside - they can even be generic! Pattern matching works very well with this concept, keeping track if all possible options are covered.

In this part:

```
match userId |> userOrError with
    | Ok username -> Console.WriteLine username
    | Error (UserNotFound id) -> Console.WriteLine $"User {id} not found"
    | Error ConnectionIssue -> Console.WriteLine $"Connection issue"
```

If I remove one of the cases, I get a compiler warning (which can be set to be elevated to an error) that pattern matching is not exhaustive. In other words, it is very easy to have complete program that handles all possible outcomes. There is also a discard pattern (`_`) that matches everything.

There is plenty of built in unions for you to use, I even used two of them in my sample: `Option<T> = Some of T | None` and `Result<TResult, TError> = Ok of TResult | Error of TError`. The both come with a set helpful functions like `Map` or `Bind`, but that's whole another topic (monads and so on).

### No nulls

`Option` replaces nulls functionally, and with pattern matching makes it impossible to have `NullReferenceExceptions`!

### Almost no noise

Another thing is, no `return` anywhere. It is just redundant. Since every line of F# code is an expression (meaning, it has a value), last executed expression in a function becomes its return value. How cool is that?

### Pipe operator - `|>`

F# comes with function currying, which really boils down to one simple fact. Every function is a one parameter function. If a function is defined with 2 parameters, when you provide the first one, you get new function with one parameter. If you give it the second parameter, you get the result. It is very nicely explained by type annotations on functions:

```fsharp
    let putStringBetweenTwoInts (int1: int) (str: string) (int2: int) : string =
        $"{int1} {str} {int2}"

    let f1: int -> string -> int -> string = putStringBetweenTwoInts
    let f2: string -> int -> string = f1 2
    let f3: int -> string = f2 "+"
    let f3_alt: int -> string =  putStringBetweenTwoInts 2 "+" // still one parameter missing
    let result: string = f3 3 // result = "2 + 3"
```

Each `->` represents one recursive step of "one parameter function returning another function".

The `|>` allows you to put what's on the left of it, into the "one parameter function" on the right.

In other words, it's very generic operator that enables you to use any function as you would use extension method. It can work, because of function currying.

### Single file

All of the code **could** be in a single file. F# does single pass compilation, which means stuff that you want to use, has to be already defined. This means two things:
- if you want to check the definition of something you see, scroll up. Always
- if you want to see entry point of a F# program, scroll to the bottom. Always


This has also one side effect (no pun intended) that needs some getting used to: the order of files in fsproj matters.

Once the main file grows too big, you can cut out a single module from it and paste it into a separate file.

# OK, but what's the point?

The point is, even though F# is very cool, it's hard to use as a default language that you build whole applications with - C# is much better suited for that purpose. 

However, we are in luck. C# and F# are both first class citizens in dotnet world, so they can interoperate with each other. It's not perfect - collection from F# are not easily accessible from C#, C# does not support discriminated unions, C# can leak nulls into F# and so on, that's why combining them directly can cause problems.

So, how do we do this? Pretty simple. We use [MediatR](https://github.com/jbogard/MediatR), write the whole infrastructure in C# (`Program.cs`, `Startup.cs`, controllers, middleware, authorization, enabling Swagger, EntityFramework contexts, repository classes) and then we write our `IRequestHandler` in F#. With this approach we force ourselves to use classes written in F# as common denominator between the languages, limit the friction that could be caused by records or discriminated unions.

In my sample code, I created the following folder structure:

```
├───FSharp.Handlers
│       Domain.fs
│       FSharp.Handlers.fsproj
│       Library.fs
│
└───WebApplication1
    │   appsettings.Development.json
    │   appsettings.json
    │   CSharpWeatherForecast.cs
    │   Program.cs
    │   Startup.cs
    │   WebApplication1.csproj
    │
    ├───Controllers
    │       WeatherForecastController.cs
    │
    ├───Handlers
    │       WeatherHandler.cs
    │
    └───Properties
            launchSettings.json
```

`WebApplication1` project has reference to `FSharp.Handlers` project.

We will start examining the code with `WeatherHandler.cs` and `Library.fs` - both files contain implementation of a handler that's doing exactly the same thing. 

C# version: 

```c#
public record GetWeatherRequest : Request<IEnumerable<CSharpWeatherForecast>>;

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

        IEnumerable<CSharpWeatherForecast> result = Enumerable.Range(1, 5)
            .Select(index => new CSharpWeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = summaries[rng.Next(summaries.Length)]
            })
            .ToArray();

        return Task.FromResult(result);
    }
}
```

I hope that there is nothing with this code and you agree that this is how you write `MediatR` handlers in C#.

Now (drum roll....) in F#!

```fsharp
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
```

F# code is literally half of the lines of C# code. Highlights are:
- inline main constructor
    - since in 99% of classes have single constructor, inlining it is a great feature that saves many lines
    - it is still possible to create more constructors, even with different visibility
- notice no `_descriptionProvider` - parameters of the constructor are just available in the whole class
- methods implement interfaces explicitly, meaning you always know whether given method belongs to the class or to one of its interfaces
- yes, IDEs have `Implement missing members` action when you specify the interface 😊

On top of those few cool features, you can use any functional technique from now on:  discriminated unions, computation expressions, pattern matching, and so on.

It also helps, that with dotnet 6 and F# 6 we will get native, built in `task` computation expression for better bridging between F# and C# worlds.

## Other cool stuff

### Consuming C# classes from F# via interfaces

```fsharp
 type IDescriptionProvider =
        abstract member Get : unit -> string seq
        
type DescriptionProvider (log : ILogger<DescriptionProvider>) =
    interface IDescriptionProvider with
        member this.Get() =
            log.LogInformation "Hello from F#!"
            [|"Freezing"; "Bracing"; "Chilly"; "Cool"; "Mild"; "Warm"; "Balmy"; "Hot"; "Sweltering"; "Scorching"|]
            |> Array.toSeq
```

As you can see, we can have `ILogger` instance as a constructor parameters without any issues. Same thing would go for `IDbContext` from EntityFramework and frankly any other dependency you need.

### Consuming F# classes from C#

```csharp
// in Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // register our handlers
    services.AddMediatR(
        typeof(FunctionalHandlers).Assembly, // F#, assembly scanning works as always
        typeof(Startup).Assembly);// C#
    // ...
    // register F# classes the same way as C# classes
    // unfortunately, this does not work that well for discriminated unions ☹
    services.AddTransient<Domain.IDescriptionProvider, Domain.DescriptionProvider>();
}

// in WeatherForecastController.cs
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
```

There is nothing stopping you from writing Controllers in F# also, but when you use `async` in methods, C# is actually more concise. I would recommend sticking to C# in the HTTP layer.

# Conclusion

As you can see, it's super easy to integrate F# and C# using `MediatR` as a bridge between the two worlds. You are able to combine the best features of both approaches, while leveraging huge dotnet ecosystem.

Jumping between C# and F# you are able to choose the best tool for given task, which gives you tons of flexibility. If you can force yourself you surface only fully compatible constructs (we do this by implementing handlers), your work becomes much easier.