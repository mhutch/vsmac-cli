# vsmac-cli

.NET CLI tool for querying Visual Studio for Mac installations and invoking bundled tools

## Installation

```bash
dotnet tool install -g vsmac-cli
```

## Examples

```
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
```

```bash
$ vsmac msbuild
Microsoft (R) Build Engine version 16.8.0 for Mono
[...]
```

## Help

```bash
$ vsmac -h
Usage:
  vsmac [options] [command]

Options:
  -p, --preview     Use preview instance of Visual Studio
  -v <v>            Use specific version of Visual Studio
  --version         Show version information
  -?, -h, --help    Show help and usage information

Commands:
  list       List available Visual Studio instances
  msbuild    Invoke the MSBuild bundled with Visual Studio
  vstool     Invoke the Visual Studio tool runner
  path       Print path to Visual Studio app bundle
  version    Print version of Visual Studio
```
