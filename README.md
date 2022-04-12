# vsmac-cli

.NET CLI tool for interacting with Visual Studio for Mac.

* Open files: `vsmac open Hello.cs`
* Query installed versions: `vsmac list`
* Find app bundle path: `vsmac path`
* Invoke MSBuild: `vsmac msbuild`
* Target specific versions: `vsmac -v 17.1 path`

## Installation

```bash
dotnet tool install -g vsmac-cli
```

## Examples

```bash
$ vsmac list
8.9.0.947 [preview] /Applications/Visual Studio (Preview).app
8.8.3.16  [stable]  /Applications/Visual Studio.app
7.4.1.48  [stable]  /Applications/Visual Studio 7.4.app
```

```bash
$ vsmac --preview path
/Applications/Visual Studio (Preview).app
```

```bash
$ vsmac version
8.8.3.16
$ vsmac --preview version
8.9.0.947
$ vsmac -v 7 version
7.4.1.48
```

```bash
$ vsmac msbuild Hello.csproj -c Release
Microsoft (R) Build Engine version 16.8.0 for Mono
[...]
```
