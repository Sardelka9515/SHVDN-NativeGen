# SHVDN-NativeGen
Source generator for SHVDN3 native hashes

This program generates C# source file **NativeHashes.cs** for ScriptHookVDotNet3 with parameters, return type and comments,
using data from [alloc8r's nativedb](https://github.com/alloc8or/gta5-nativedb-data/).
Pre-generated file can be found in the main folder, along with source code.

# Options 

You can specify generating options with commandline, e.g.
```
SHVDN-NativeGen.exe Parameters Returns
```


## None 
Only generate native name and hash.

## Parameters 
Includes parameter info in the summary section

## Returns 
Include return type in returns section

## Comments 
Include comments in the remarks section.

## OldNames 
Generate legacy native name with same hash

## MarkObsolete 
Make old native name as obsolete and points to the updated one in remarks. 

## All 
All the above, the default generating config.
