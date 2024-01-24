# VSMacLocator

API for enumerating and locating installed Visual Studio for Mac instances.

The `VSMacLocator.FindAll()` method returns a collection of `VSMacLocator.VSMacInstance` objects
that have the following properties:

| Property | Value |
| --- | --- |
| `BundlePath` | Path of the app bundle. |
| `BinDir` | Directory within the app bundle containing the managed binaries. |
| `BundleVersion` | Version of the app bundle. |
| `ReleaseId` | Internal release identifier string of the Visual Studio for Mac build. |
| `IsPreview` | Whether the app bundle is a preview release. |
| `MSBuildDllPath` | The path to the `msbuild` executable inside the app bundle. |
| `VSTool` | The path to the `vstool` executable inside the app bundle. |

## Example

```csharp
foreach (VSMacInstance instance in VSMacLocator.FindAll()) {
    Console.WriteLine(${instance.BundlePath} = {instance.Version}");
}
```