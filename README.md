# FlatSharpDelta
FlatSharpDelta is an extension to the [FlatSharp](https://github.com/jamescourtney/FlatSharp) serialization library that allows you to automatically track changes made to your FlatSharp objects and produce a delta object based on those changes.

At its core, FlatSharpDelta adds the following 3 methods to your FlatSharp objects:

- `GetDelta`: Returns a delta object containing the changes that have been made to the object since `UpdateReferenceState` was last called, or since the object was created.
			The delta object can be serialized just like any other FlatSharp object.
			The delta object is optimized to contain only the data necessary to construct the current object state when serialized.
			The method scans through all properties (including nested objects) and makes sure to only include the properties that differ from the reference state.
			FlatSharpDelta also tracks all changes made to lists, and will remove any redundant information (such as adding and then removing an item).
			If no changes have been made to the object, this method returns `null`.<br/><br/>Note that this method always returns the same object instance, and that it may change as you make changes to your object.
			This means that the delta object is only valid until further changes are made to the object.
			The method needs to be called again after changes have been made, in order to obtain a valid delta object.</pre>

- `ApplyDelta`: Applies the changes present in a delta object.

- `UpdateReferenceState`: Sets the current state of the object as the <i>reference state</i>. The `GetDelta` method uses the reference state as a base for comparison when checking which properties in the object that have changed.

## Usage
 
The FlatSharpDelta compiler can be installed from [NuGet](https://www.nuget.org/packages/FlatSharpDelta.Compiler).
 
After installing the compiler with NuGet, you can add your `fbs` files like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <FlatSharpDeltaSchema Include="Hello.fbs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FlatSharpDelta.Compiler" Version="1.0.1">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FlatSharp.Runtime" Version="7.1.0" />
  </ItemGroup>
</Project>
```
 
Notice that the `fbs` input file uses the `FlatSharpDeltaSchema` instead of the `FlatSharpSchema` tag.<br/>
Also note that the `FlatSharp.Runtime` package also needs to be installed as FlatSharpDelta generates code that requires FlatSharp's runtime library to work.
 
### Code Example
 
The following example shows an `fbs` file and an example of how to use the methods mentioned above:
 
```
namespace HelloWorld;

table Item {
    Name : string;
}

table Player {
    Health : int;
    Items : [Item];
}
```
 
```csharp
Player playerClient = new Player { Health = 100, Items = new ItemList() };
Player playerServer = new Player(playerClient);

playerClient.Health = 50;
playerClient.Items.Add(new Item { Name = "Sword" });

PlayerDelta delta = playerClient.GetDelta();
playerServer.ApplyDelta(delta);
// playerClient and playerServer now look the same.

playerClient.UpdateReferenceState();
// calling playerClient.GetDelta() at this point would return null because nothing changed.
```

## Compatibility

### Compatibility with .NET
This package has the same .NET compatibility as FlatSharp. Please see the [FlatSharp wiki page](https://github.com/jamescourtney/FlatSharp/wiki/Compiler) for more information.

### Compatibility with the FlatSharp compiler
As FlatSharpDelta makes use of the `FlatSharp.Compiler` package internally, the intention is for FlatSharpDelta to always make use of the latest version of `FlatSharp.Compiler` (`7.1.0` as of this writing). This means you should use the same version for the FlatSharp.Runtime package. As new major (and potentially breaking) versions of FlatSharp are published, it might take a while before the FlatSharpDelta compiler is up to speed with the new version of the FlatSharp compiler.

## Noteworthy things

- Because this library needs to keep track of the changes you make to lists/vectors, all types defined in your fbs files will generate a custom list class. A table called `Player` will generate a `PlayerList` class, for example. All vector fields you define in your fbs files will use this custom list class of the corresponding type. This also means that the "IList" vector type is the only supported vector type (other vector types such as "Memory" are not supported). The custom list class implements `IList<T>`.
- The custom list types have a `Move` method. You should use this method instead of removing and then adding an item to the list. This can greatly reduce the amount of data from serializing the delta object returned from `GetDelta`.
- You should avoid calling `UpdateReferenceState` on nested objects, as doing so will screw up the reference state of the root object. In general, you should only ever need to call `UpdateReferenceState` on the root object.

## License & Credits
FlatSharpDelta is licensed under Apache 2.0.

Credits for the [FlatSharp](https://github.com/jamescourtney/FlatSharp) library goes to creator James Courtney and its contributors.
