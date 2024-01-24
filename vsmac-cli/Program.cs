// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

using VSMacLocator;

var instances = VSMacInstance.FindAll();

if (instances.Count == 0)
{
    Console.WriteLine("No instances found");
    return 1;
}

var rootCommand = new RootCommand();
var previewOption = new Option<bool>("--preview", "Use preview instance of Visual Studio");
previewOption.AddAlias("-p");
rootCommand.AddOption(previewOption);

var specificVersionOption = new Option<string>("-v", "Use specific version of Visual Studio");
rootCommand.AddOption(specificVersionOption);

VSMacInstance? GetInstance(ParseResult p)
{
    // https://github.com/dotnet/command-line-api/issues/1360
    var valueForSpecificVersionOption =
        p.HasOption(specificVersionOption)? p.ValueForOption(specificVersionOption) : null;

    if (valueForSpecificVersionOption is string specificVersion)
    {
        var matches = instances.Where(i => i.BundleVersion.StartsWith(specificVersion, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"Did not find any version matching '{specificVersion}'");
            return null;
        }
        else if(matches.Count > 1)
        {
            Console.Error.WriteLine($"Found multiple versions matching '{specificVersion}': {string.Join(", ", matches.Select(m => m.BundleVersion))}");
            return null;
        }
        else
        {
            return matches[0];
        }
    }

    var usePreview = p.ValueForOption(previewOption);
    if (instances.FirstOrDefault(i => i.IsPreview == usePreview) is VSMacInstance instance)
    {
        return instance;
    }

    Console.Error.WriteLine($"Did not find a {(usePreview?"preview":"stable")} version of Visual Studio");
    return null;
}

rootCommand.Add(new Command("list", "List available Visual Studio instances") {
    Handler = CommandHandler.Create(() => {
        var maxLen = instances.Max(s => s.BundleVersion.Length);
        foreach (var instance in instances)
        {
            Console.WriteLine($"{instance.BundleVersion.PadRight(maxLen)} {(instance.IsPreview ? "[preview]" : "[stable] ")} {instance.BundlePath}");
        }
    })
});

rootCommand.Add(new SubprocessCommand<VSMacInstance>(FindTool, "msbuild", "Invoke the MSBuild bundled with Visual Studio") {
    Kind = SubprocessKind.Mono
});

rootCommand.Add(new SubprocessCommand<VSMacInstance>(FindTool, "vstool", "Invoke the Visual Studio tool runner"));

rootCommand.Add(new Command("path", "Print path to Visual Studio app bundle") {
    Handler = new VSInstanceCommandHandler(GetInstance, i => { Console.WriteLine(i.BundlePath); return 0; })
});

rootCommand.Add(new Command("version", "Print version of Visual Studio") {
    Handler = new VSInstanceCommandHandler(GetInstance, i => { Console.WriteLine(i.BundleVersion); return 0; })
});

rootCommand.Add(new OpenCommandHandler("open", "Opens the specified files with Visual Studio"));

var builder = new CommandLineBuilder(rootCommand);

SubprocessCommand<VSMacInstance>.RegisterMiddleware(builder, GetInstance);

builder.UseDefaults();
var parser = builder.Build();
return parser.Invoke(args);

static string? FindTool(VSMacInstance instance, string toolName)
{
    var processPath = toolName switch
    {
        "msbuild" => instance.MSBuildDllPath,
        "vstool" => instance.VSToolPath,
        _ => null
    };
    if (processPath == null || !File.Exists(processPath))
    {
        Console.Error.WriteLine($"Did not find '{toolName}' in Visual Studio {instance.BundleVersion}");
        return null;
    }
    return processPath;
}
