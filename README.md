# SHVDN-NativeGen
Source generator for [ScriptHookVDotNet](https://github.com/crosire/scripthookvdotnet)

This program generates C# source file **NativeHashes.cs** and for [ScriptHookVDotNet](https://github.com/crosire/scripthookvdotnet) with parameters, return type and comments,
using data from [alloc8or's nativedb](https://github.com/alloc8or/gta5-nativedb-data/).
Pre-generated file can be found in the main folder, along with source code.

# Features
View the info about a native hash right in your IDE:
![image](https://user-images.githubusercontent.com/106232474/201892090-e072be80-7734-461d-8b2d-6e3a16dd0671.png)

Strong-typed invoker allowing you to call native like normal functions:

```
// Add this line at the top of you source file
using static GTA.Native.NativeInvoker;

// Then you can call it like this
Entity killer = GET_PED_SOURCE_OF_DEATH(Game.Player.Character);
```
Comment is fully supported as well:
![image](https://user-images.githubusercontent.com/106232474/201893992-96fc5e7d-b90c-4ad4-870c-940e7b296d89.png)



# Options 

You can specify generating options with commandline, e.g.
```
SHVDN-NativeGen.exe Parameters Returns
```

See [this enum](https://github.com/Sardelka9515/SHVDN-NativeGen/blob/9def67a53d42eeac0fae4eb113345b96d6c91ef6/SHVDN-NativeGen/Types.cs#L10) for all supported options
