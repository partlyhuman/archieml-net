### WARNING: THIS LIBRARY IS NOT YET BATTLE-TESTED. However, it passes 100% of the official ArchieML test suite. Use at your own discretion and please file any bugs you may run into!

# ArchieML.Net

An [ArchieML](http://archieml.org) parser for .Net 3.5 and up, including test suite, commandline conversion tool, and Unity3d package.

>ArchieML is a structured text format optimized for human writability.

To learn more about ArchieML visit its [official site](http://archieml.org). This .Net parser was created to bring the non-technical user-friendliness of ArchieML to .Net, Mono, and Unity projects. Its author realized that externalizing settings and content in XML, YAML, and JSON formats through the years never accomplished the goal of allowing the non-technical end user confidence to make changes without breaking the software.

This library is a rewrite, rather than a direct port of the official parser, [archieml-js](https://github.com/newsdev/archieml-js).

The library depends on [JSON.Net](https://github.com/JamesNK/Newtonsoft.Json). Since Archie can model dynamic data in a strict subset of what JSON can, the author used the existing tree data structures of Json.Net to jumpstart development. As a bonus, the Json.Net library includes a wonderful type mapping system, allowing you to map JSON, and now by extension ArchieML, directly to .Net types, whether these are your own custom value objects, or generics like `Dictionary<string, object>`. The `Archie` class converts files, streams, or text, into a top level `JObject` instance (a data structure used by Linq to JSON in JSON.Net).

From a `JObject`, you can very conveniently [query the data using LINQ extension methods](http://www.newtonsoft.com/json/help/html/QueryingLINQtoJSON.htm), or convert it to a .Net object, whether this is an atomic value, collection type, or custom class, using [ToObject](http://www.newtonsoft.com/json/help/html/ToObjectComplex.htm).

## aml

Included is a commandline tool, aml.exe, which converts ArchieML to JSON or XML on the command line. It can be used with files or standard in. Run with `-h` to see usage.

## Unity package

Only the two .dll files included in the `Plugins/` directory are required. The `Test/` directory just shows an example use and can be safely deleted.

-- Roger Braunstein, @partlyhuman 2015
