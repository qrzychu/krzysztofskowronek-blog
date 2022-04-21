open System.Collections.Generic
open System.Globalization
open System.Threading

Thread.CurrentThread.CurrentCulture <- CultureInfo("en-US")
Thread.CurrentThread.CurrentUICulture <- CultureInfo("en-US")

let elements: seq<string * string> = Seq.init 5 id |> Seq.map (fun i -> ($"key {i}", $"value {i}"))

let elementsInline: (string * string) list = [("key", "value"); ("key 2", "value 2")]

let fsharpDictionary: IDictionary<string,string> = elements |> dict

let dictionary: Dictionary<string,string> = fsharpDictionary |> Dictionary

dictionary.Add("new key", "new value")

let map = elements |> Map

let newMap = map |> Map.add "new key" "new value"

let filteredMap = map
                    |> Seq.filter (fun (i : KeyValuePair<string,string>) -> i.Key.Contains("something") && i.Value > "a")
                    |> Seq.map (fun i -> (i.Key, i.Value))
                    |> Map.ofSeq