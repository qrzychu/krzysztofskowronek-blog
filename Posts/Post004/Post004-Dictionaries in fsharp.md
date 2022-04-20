# Intro

When you first pickup F# as a C# programmer, there is one area that feels a bit unintuitive - dictionary usage. Following your C# instincts and IntelliSense will cause some headaches.

In this post we will explore why, how to solve those headaches and why it's not as bad as it sounds.

# Dictionary usage sample

We will start with a sequence of keys and values to fill out the dictionary:

```f# script
let elements: seq<string * string> = Seq.init 5 id |> Seq.map (fun i -> ($"key {i}", $"value {i}"))

// or inline initalization
let elementsInline: (string * string) list = [("key 1", "value 1"); ("key 2", "value 2")]
```

Then, using IntelliSense exploration, we find how to construct the dictionary:

```f# script
// type annotation is exactly what the compiler inferred
let dictionary: System.Collections.Generic.IDictionary<string,string> = elements |> dict
```

As you can see, the compiler tells us that now we work `IDictionary<string, string>` interface that we know from C#. This means, that we can write the following code and it will compile: 

```f# script
dictionary.Add("new key", "new value")
```

Here wa hit the first headache: the `Add` method returns `void`, which means it wants to modify the dictionary. We are in F# - we want it to be immutable. Fortunately, when we run this code, we get an exception:

`System.NotSupportedException: This value cannot be mutated`

So what is our instinct? Add mutable keyword to the dictionary:

```f# script
let mutable dictionary: System.Collections.Generic.IDictionary<string,string> = elements |> dict

dictionary.Add("new key", "new value")
```

But the same thing happens. Why? Well, the `mutable` keyword just lets us to overwrite the `dictionary` value with new instance, which is not what we want. Maybe the full type of the object will tell us more:

```f# script
let dictionary: System.Collections.Generic.IDictionary<string,string> = elements |> dict

dictionary.GetType()
```

gives us the following: ```Microsoft.FSharp.Core.ExtraTopLevelOperators+DictImpl`3[Microsoft.FSharp.Core.CompilerServices.RuntimeHelpers+StructBox`1[System.String],System.String,System.String]```

This isn't our good old C# dictionary, but a F# specific proxy blocking all of our mutating operations. Let's try another trick and create C# dictionary explicitly by calling its constructor:

```f# script
let dictionary: Dictionary<string,string> = elements |> dict |> Dictionary

dictionary.Add("new key", "new value")
```

Now it works! Unfortunately, now we have a mutable dictionary, but at least we already know how to use it.

# What is the F# native solution?

It is called `Map`, and can be used as follows:

```f# script
let map = elements |> Map

let newMap = map |> Map.add "new key" "new value"
```

Map offers all operations you expect: adding, removing, checking if key exists and so on. It also can be used as a sequence, just like `Dictionary` in C#:

```f# script
let filteredMap = map
                    |> Seq.filter (fun (i : KeyValuePair<string,string>) -> i.Key.Contains("something") && i.Value > "a")
                    |> Seq.map (fun i -> (i.Key, i.Value)) // sadly, we need to map the KeyValuePair to a tuple
                    |> Map.ofSeq
```

# What is the difference between `Map` and `Dictionary`? When to use which?

To answer that, let's check the documentation:  [Collection types](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/fsharp-collection-types)

In here we have a nice table that describe what each of the built-in collection type has to offer.

As we can see, there are 5 major types:

- `seq` - abstraction over a collection, supports lazy generation. It is an alias for `IEnumerable<T>`
- `List` - immutable linked list of elements of the same type
- `Array` - fixed sized, mutable collection - pretty much an alias for C# array
- `Map` - immutable key-value collection, similar to `Dictionary` form C#
- `Set` - immutable collection of unique values. By default, it uses F# structural comparison, can use `IComparable`

Below that, there is a table of functions and their complexity for given collection type. For example, `add` operation for `Map` type has `O(log(N))` complexity, so does `containsKey`.

This is different than `O(1)` for `Dictionary`, right? That's because the `Map` type is actually not a dictionary, but a binary tree (as are most immutable structures).

In [documentation for `Map`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-fsharpmap-2.html) we can read:
```
Immutable maps based on binary trees, where keys are ordered by F# generic comparison. By default comparison is the F# structural comparison function or uses implementations of the IComparable interface on key values.

See the Map module for further operations on maps. All members of this class are thread-safe and may be used concurrently from multiple threads.
```

So, what do we loose with `Map`? We loose performance for finding elements and adding new ones.

What do we get? Immutability and thread-safety.

# Conclusion

F# has a type called `Map`, which represents an immutable key-value collection. However, under the hood, it is implemented as a binary tree, which has performance implications. Checking if the key exists or getting an element under given key are no longer `O(1)` operations, they are `O(log(N))`.

Fortunately, we can still access `System.Collections.Generic.Dictionary` when we need that dictionary characteristic performance. We can also use F# built-in `dict`, which wraps the mutable dictionary in an immutable construct, while still having constant complexity key operations. Unfortunately, that construct is immutable just in runtime, we still can call `dictionary.Add` and the code will compile.  

Personally, I am quite happy with all of these options, because:

- when you value immutability, you use `Map`
- when you want that constant access complexity, you can still get it with `dict`
- for situations where a mutable dictionary offers best performance, you use C# implementation: `elements |> dict |> Dictionary`

I hope this short exploration will be helpful for people trying out F# and hitting the dictionary wall, like I did some time ago.



