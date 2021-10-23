# Intro

For last year or so I really got interested in functional approach to programming. I started by watching brilliant Richard Feldman talks on Elm and fell in love with the concepts. Beeing dotnet developer I discovered F# ,which to my delight, turned out to be Elm on steroids.

However, functional ways proved to be pretty hard to integrate into existing codebase, and I wasn't really ready to start new project using F# only. 

# "Cons" to functional programming

This may turn out to be a bit controversial, but I don't like functional ways of setting up projects. In functional world, you don't use Dependency Injection, you don't use containers and you don't use mocking for testing. 

For all of that, you use functions, partial applicaiton or IO monad. Those facts on their own are not cons - they are in most cases a pro. For starters, partial application gives you compile time checks if your dependencies are set up correctly. IO monad gives you clear, type-system level indication about functions causing side effects.

All of that is very neat, and you can even elevate your inner pride by throwing around the m-word, but in a real world, it does not compose very well with industry stadards and can get very hard to manage with in bigger projects with multiple depndencies.

For example, if you want to create a REST API in F#, you would use [Giraffe](https://github.com/giraffe-fsharp/Giraffe). It's great, really. 

According to benchmarks, it's one of the most performant ways to use ASP.NET Core. 

It lowers your mental load by reducing number of concepts you need to work with - no more controllers, middleware, filters etc. Everything is just a simple, composable `IHttpHandler` - type of middleware.

I may go into detail on that in future, but for now I will redirect you to a great talk on the subject: https://www.youtube.com/watch?v=JuIq7mU50jA

What are the cons then? Well, Swagger? No. Scoped EF Core context? Yeah, you can use it, but it feels wrong, since you literally use ASP.NET Core container to get it. 

Here you can find a sample blog application: https://github.com/ChrisPritchard/GiraffeBlog

Unfortunately, most of the "good stuff" in dotnet is made with OO in mind, designed to be consumed from C#.

# Pros of functional programing with F#

The biggest pro is concisness - you can see much bigger part of your program on a single screen. There are few reasons for that:
- no curly braces - you save tons of lines by that
- `Bind` and `Map` functions - no more boilerplate ifs. Imagine using LINQ for the whole program, and now try to remember when was the last time you checked if the count was 0 in or before LINQ pipeline
- no `return` keyword (almost, you still use it in computation expressions)
- pattern matching

Below you will find two code blocks: one in F#, the other one in C#. They do exactly the same thing, they are "getting" a user from somewhere, and if user is found, they lowercase the username. Pretty simple.

Both represent how something like this would be achieved in a real application, given that lowering the name would be some more complex task. Remember, that in C# convention is to put every class, interface etc in a seperate file. 

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
                Console.WriteLine("Error occured: " + e.Message + Environment.NewLine + e.StackTrace);
            }
        }
    }
}
```

### F# code

```
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
- Hashnode does not support syntax higlight for F# :(
- F# code takes up 38 lines and at most 74 columns
- C# code takes up 83 lines and at most 98 columns
- C# code in reality would be split into 6 files

I am fully aware that in both languages solution to this problem could be one or two liners, but I'm not aiming for minimal possible code. I'm aiming for "how it's done" in big project, with testabillity and maintainability in mind.

I would like to point out few really cool things about F# though.

### Discriminated unions and pattern matching

First feature to higlight is Discriminated Union (coming to C# soon, fingers crossed!).

```
    type SomeErrorType =
        | UserNotFound of int
        | ConnectionIssue
```

It's like an enum, but each option can be of different type (even another discriminated union!) and hold information inside - they can even be generic! Pattern matching works very well with this concept, keeping track if all possbile options are covered.

In this part:

```
match userId |> userOrError with
    | Ok username -> Console.WriteLine username
    | Error (UserNotFound id) -> Console.WriteLine $"User {id} not found"
    | Error ConnectionIssue -> Console.WriteLine $"Connection issue"
```

if I remove one of the cases, I get a compiler warning (which can be set to be elevated to an error) that pattern matching is not exahaustive. In other words, it is very easy to have complete program that handles all possible outcomes. There is also a discard pattern (`_`) for lazy people, that matches everything. 

There is plenty of built in unions for you to use, I even used two of them in my sample: `Option<`T> = Some of T | None` and `Result<`TResult, `TError> = Ok of TResult | Error of TError`. The both come with a set helpful functions like `Map` or `Bind`, but that's whole another topic (monads and so on).

### No nulls

`Option` replaces nulls functionally, and with pattern matching makes it impossible to have `NullReferenceExceptions`!

### Almost no noise

Another thing is, no `return` anywhere. It is just redundant. Since every line of F# code is an expression (meaning, it has a value), last executed expression in a function becomes its return value. How cool is that?

### Pipe operator - `|>`

F# comes with function currying, which really boils down to one simple fact. Every function is one parameter function. If a function is defined with 2 parameters, when you provide the first one, you get new one parameter function. If you give it the second parameter, you get the result. It is very nicely explained by type annotations on functions:

```
    let putStringBetweenTwoInts (int1: int) (str: string) (int2: int) : string =
        $"{int1} {str} {int2}"

    let f1: int -> string -> int -> string = putStringBetweenTwoInts
    let f2: string -> int -> string = f1 2
    let f3: int -> string = f2 "+"
    let f3_alt: int -> string =  putStringBetweenTwoInts 2 "+"
    let result: string = f3 3
```

Each `->` represents one recursive step of "one parameter function returning another function".

The `|>` allows you to put what's on the left of it, into the "one parameter function" on the right.

In other words, it's very generic operator that enables you to use any function as you would use extension method.

### Single file



## OK, but what's the point?

The point is, even though F# is very cool, it's hard to use as a default language that you build whole applications with - C# is much better suited for that purpose. 

However, we are in luck. C# and F# are both first class citizens in dotnet world, so they can interoparate with each other. It's not perfect - collection from F# are not easily accessible from C#, C# does not support discriminated unions, C# can leak nullls into F# and so on.

So, how do we do this? Pretty simple. We use [MediatR](https://github.com/jbogard/MediatR). We write the whole infrastructure in C# (`Program.cs`, `Startup.cs`, controllers, middleware, autorization, enabling Swagger, EntityFramework contexts, repository classes) and then we write our `IRequestHandler` in F#.