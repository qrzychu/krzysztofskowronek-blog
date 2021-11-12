# The problem

My father works as an IT specialist and recently encountered a weird problem. Laptops in their WiFi network had very slow network connectivity. After some digging, it turned out that in some places (like at one specific corner of a desk), laptops were jumping between 2.4GHz and 5GHz networks. Solution is easy - pin them to one of the networks by setting preferred bandwidth on their network card.

The problem is that unless users complain directly, you don't know which laptops to target. In this particular network, devices use certificates to log into Radius server (MS implementation). The server keeps logs of the events, but the events are plenty due to thousands of devices in the network. Since manual analysis was virtually impossible, he asked me to write a quick program to find users and laptops affected by this problem.

## The input

Each line of the log is a separate XML document with numerous properties. The set of properties depends on what event is logged, so not all event are of use in our case. Here is a sample line from the log, truncated to just needed information and formatted for readability:

```xml
<Event>
    <Event-Timestamp data_type="4">10/13/2021 13:01:33</Event-Timestamp>
    
    <!-- duration of session in seconds -->
    <Acct-Session-Time data_type="0">1453</Acct-Session-Time> 
    
    <!-- MAC address of the laptop -->
    <Calling-Station-Id data_type="1">00-11-22-33-44-55</Calling-Station-Id> 

    <User-Name data_type="1">johndoe@company.com</User-Name>
</Event>
```

We want to find users that have many short lived sessions, so our program will have 3 arguments:

- *-f* - path to the log file
- *-t* - maximum session duration
- *-c* - minimum session count per day

## The algorithm

We will follow this recipe to get our data:

1. read all lines and try to extract the 4 nodes we need, ignore the rest
2. filter out all sessions that are longer than *-t* parameter, if present
3. group the remaining sessions by user, than calculate how many sessions given user had in each day based on timestamp
4. discard all users that had fewer sessions than *-c* parameter in any given day
5. list remaining users along with their MAC address and in what day they had the most sessions

Algorithm is quite simple, but the log files can be of gigabytes in size, so we need something reasonably fast. C# should be more than enough for this task.

## The output

I decided to format the output in the following way:

```text
User: johndoe@company.com, MAC: 00-11-22-33-44-55
10.10.2021: 4 sessions. Shortest: 3s,3s,3s,3s
11.10.2021: 104 sessions. Shortest: 1s,2s,2s,3s,3s
12.10.2021: 115 sessions. Shortest: 3s,3s,3s,3s,3s
13.10.2021: 44 sessions. Shortest: 0s,0s,3s,3s,3s
14.10.2021: 77 sessions. Shortest: 0s,3s,3s,3s,3s
```

As you can see, we will have the user with their MAC address (we assume one per user for now), and then list of dates when they had most sessions, with top 5 shortest session durations.

# The code

## Command line parameters

I am a huge fan of [CommandLineParser](https://github.com/commandlineparser/commandline) library. It hides the whole process of parsing command line arguments from you, so you can just focus on implementing your logic. It even supports verbs, so you can easily code something like `git add -a -c "comment"`.

My options class:

```csharp
 public class Options
{
    [Option('f', "file", Required = true, HelpText = "Path to the log file")]
    public string File { get; set; } = "";

    [Option('c', "sessionCount", HelpText = "Min session count per day")]
    public int? MinSessionCount { get; set; }

    [Option('t', "sessionTime", HelpText = "Max session duration")]
    public int? MaxSessionDuration { get; set; }
}
```

That's pretty straightforward. Usage is even simpler:

```csharp
static void Main(string[] args)
{
   Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opt =>
                {
                    // code goes here only if arguments are valid
                    Console.WriteLine(
                            LoadRecords(opt.File).Run(opt).ToDisplay()
                        );
                });
// if arguments are not valid, help will be displayed
}
```

That's it. `CommandLineParser` will even generate a very nice help screen and will give the user easy to understand error if mandatory arguments are missing. Since everything is in a single static class, I made use of extension methods to get a "pipeline-like" feel that I really like in F#.

## Reading the file

As I said in the intro, the log files can be very sizable, and what's even worse, should not be locked while our program is running, since the Radius server will constantly log new events. That means that your greatest friend `File.ReadAllLines` function is not an options, because the log will not fit into memory. Also, this function actually locks the file for the read.

We need `IEnumerable<string>`, which will lazily yield each line separately and will not lock the file. Using features of C#9 and dotnet 5, I ended up with the following, quite elegant function:

```csharp
private static IEnumerable<string> ReadLines(string path)
{
    using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    using var reader = new StreamReader(file);

    while (!reader.EndOfStream) yield return reader.ReadLine()!;
}
```

## Parsing the data

I created a C# record to represent each session and a function to parse each XML document:

```csharp
public record Record(DateTime Timestamp, int Duration, string MAC = "", string User = "");

private static IEnumerable<Record> LoadRecords(string filePath) => 
    ReadLines(filePath)
    .AsParallel() // helps a little, but it's basically for free
    .Select((x, i) =>
    {
        try
        {
            var doc = XDocument.Parse(x);

            // I was sad to learn that I cannot use property initialization (works in C# 10 though)
            return new Record(
                GetTimestamp(doc),
                GetDuration(doc),
                GetElement("Calling-Station-Id", doc),
                GetElement("User-Name", doc).ToLower().Trim()
            );
        }
        catch (Exception)
        {
            // this means that given line does not contain data we need
            return null; 
        }
    })
    .Where(x => x is not null)
    // this is to let the compiler know that we return IEnumerable<Record> and not IEnumerable<Record?>
    .Select(x => x!); 
```

As you can see, we ignore all errors - in this use case it does not matter if we count 1000 or 1002 sessions, so we don't care that much about error handling. I also love that I can throw in `AsParallel()` and see performance gains, especially for huge files - it feels like cheating.

Next functions on our radar are `GetElement`, `GetTimestamp` and `GetDuration`:

```csharp
static string GetElement(string name, XDocument doc)
{
    // System.Xml.Linq namespace is not annotated for nullable references, so we need few bangs!
    return doc!.Root!.Elements(XName.Get(name)).FirstOrDefault()?.Value ?? "";
}

static int GetDuration(XDocument doc)
{
    string durationString = GetElement("Acct-Session-Time", doc);

    return int.TryParse(durationString, out int duration)
        ? duration
        : throw new Exception();
}

static DateTime GetTimestamp(XDocument doc)
{
    string textTimestamp = GetElement("Event-Timestamp", doc)!;

    return DateTime.TryParseExact(textTimestamp, "MM/dd/yyyy HH:mm:ss", null, DateTimeStyles.None,
        out DateTime result)
        ? result 
        : throw new Exception();
}
```

All of the above functions are quite straightforward. I throw exceptions in case of failed parsing of dates and ints - I don't like that, but passing null and checking it just one level up in a try/catch block made me lazy. Also, I really prefer F# `Option` type for problems like this.

## Filtering the data

```csharp
public static IEnumerable<Record> Run(this IEnumerable<Record> records, Options opt)
{            
    if (opt.MaxSessionDuration is not null)
    {
        records = FilterByMaxSessionDuration(records, opt.MaxSessionDuration.Value);
    }

    if (opt.MinSessionCount is not null)
    {
        records = FilterByMinSessionCount(records, opt.MinSessionCount.Value);
    }

    return records;
}
```

The `Run` function is the heart of this program, and is super simple. `FilterByMaxSessionDuration` does just what you expect, so I will not post it.

The `FilterByMinSessionCount` is the most complicated piece of code I wrote for this problem:

```csharp
private static IEnumerable<Record> FilterByMinSessionCount(IEnumerable<Record> records, int minSessionCount)
{
    return records
        .GroupBy(x => x.User.ToLower()) // group by user
        .Where(x => // filter the groupings
                x.GroupBy(r => r.Timestamp.Date) // for each user group sessions by date
                    .Any(r => 
                        // count sessions per day
                        r.Count() >= minSessionCount) 
                ) // remove all sessions for a user, that never had more than minSessionCount per day
        .SelectMany(x => x); // unfold the grouping, so we get raw sessions back
}
```

It's very short, but when you think about it, it does quite a lot. I'm really glad that we have compilers and languages like C# that so we can express this logic in 6 lines of code.

## Displaying the results

As you remember, we designed very readable output format, so here is how we can achieve it:

```csharp
private static string ToDisplay(this IEnumerable<Record> records)
{
    IEnumerable<string> result = records.GroupBy(x => x.User, x => x)
        .Select(x =>
            $"User: {x.Key}, MAC: {x.First().MAC} {Environment.NewLine}{x.GetSessionsDescription()}");

    return string.Join(Environment.NewLine + Environment.NewLine, result);
}

static string GetSessionsDescription(this IEnumerable<Record> records)
{
    var sessionsPerDay = records.GroupBy(x => x.Timestamp.Date);
    var builder = new StringBuilder();

    foreach (var sessions in sessionsPerDay)
    {
        var shortest = sessions.OrderBy(x => x.Duration).Take(5).Select(x => $"{x.Duration}s").ToArray();
        builder.Append(
            $"{sessions.Key.ToString("d")}: {sessions.Count()} sessions. Shortest: {String.Join(",", shortest)}");
        
        builder.Append(Environment.NewLine);
    }

    return builder.ToString();
}
```

As you can see, combination of LINQ and string interpolation makes quite terse and nice code.

# Conclusions 

In around 150 lines of code, we achieved a lot:

## Streaming

By passing `IEnumerable<Record>` through the whole program, we take advantage of the lazy nature of sequences. We can process log file of any size (worked on 4GB file), while the program stays under 25MB of RAM usage. Adding `AsParallel` does not require any more changes and gives a lot of performance, especially if the disk is fast and the program is not I/O bound.

## Performance

The tool also seems reasonably quick - for 200MB file with almost 20k of records it takes 1.5s to run the whole program on my machine (Ryzen 7 4850u). In "production" tests it took under 30s to parse gigabytes of logs, which means that it runs as fast as it reads data from disk.

## Usability

Thanks to `CommandLineParser` in 15 lines of code we have fully fledged CLI application, with help screen and error messages.

# Issues

While I really like the end result, the process to get there was not as smooth as I wanted.

## Nullable references

First of all, nullable references are a great feature, when they work. However, in my short experience with them, they rarely do. If you use not annotated library (like `System.Xml.Linq`), you have to sprinkle `!` around, which by the way is just a decoration for the compiler to let go of the analysis. You can still get NREs!

Also, funny thing. `(null!)` is correct code and compiler treats it as not a null 😊

## Procedural nature

Even though every function is a static function and can be an extension method, it did not use that a lot. To be honest, I refactored the functions to extensions while I was writing a complaint that programing sucks without pipe operator. I will make a post about how to make something akin to the pipeline operator in C# inspired by `let` function Kotlin, but for now it's not here.

This plus the possibility to write the functions in any order made me end up with quite a spaghetti code. Yes, I was able to write 10 function program that has 150 lines and make it confusing. I needed to refactor it, move functions up and down, add `this` in some. Also, I would never do the refactoring if I wasn't writing this post.

# Solution to the issues?

Yes, there is a system wide solution. **It's F#.**

I picked C# for this project, because I had just one evening to do it, and I'm still much more fluent in it than F#. However, while writing I found myself wanting tons of F# features:

- `Option` type
- monadic bind
- `|>` pipe operator
- type inference

Here is a sample function from F# implementation of the exact same program:

```fsharp
let run (options: Options) =
    readLines options.File
    |> loadRecords
    |> filterByMaxSessionDuration options.MaxSessionDuration
    |> filterByMinSessionCount options.MinSessionCount
```

It just flows, and this is the first thing you write. It feels natural to do it this way. You really feel like you are falling into [pit of success](https://www.youtube.com/watch?v=US8QG9I1XW0). If you don't believe that functional approach is beneficial, go ahead and rewrite this program without LINQ. It's hard to imagine the for loops you would need to express the same logic. Also, I think recreating the `IEnumerable` streaming behavior would quite a lot of brainpower.

F# version also performs **EXACTLY THE SAME!**, so we don't pay for it in the runtime.

```markdown
| Method |    Mean |    Error |   StdDev |
|------- |--------:|---------:|---------:|
| CSharp | 1.456 s | 0.0557 s | 0.0368 s |
| FSharp | 1.373 s | 0.0688 s | 0.0455 s |
```

Rewriting this program in F# gave me idea for a whole series, where I will write the same thing in different languages and compare their syntax, tooling (that's rarely done) and performance. F# will be described in the next post, later I think I will tackle Kotlin, Rust, Python, maybe Go and node.js. There is plenty of tools to choose from.

Stay tuned for next posts in this series!
