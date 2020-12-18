// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;

using VSMacLocator;

class Program
{
    static int Main(string[] args)
    {
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

        VSMacInstance GetInstance(ParseResult p)
        {
            VSMacInstance instance;

            var specificVersion = p.ValueForOption(specificVersionOption);
            if (specificVersion != null)
            {
                var matches = instances.Where(i => i.BundleVersion.StartsWith(specificVersion)).ToList();
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
            else
            {
                var usePreview = p.ValueForOption(previewOption);
                instance = instances.FirstOrDefault(i => i.IsPreview == usePreview);
                if(instance == null)
                {
                    Console.Error.WriteLine($"Did not find a {(usePreview?"preview":"stable")} version of Visual Studio");
                }
            }
            return instance;
        }

        var listCommand = new Command("list", "List available Visual Studio instances") {
            Handler = CommandHandler.Create(() =>
            {
                var maxLen = instances.Max(s => s.BundleVersion.Length);
                foreach (var instance in instances)
                {
                    Console.WriteLine($"{instance.BundleVersion.PadRight(maxLen)} {(instance.IsPreview ? "[preview]" : "[stable] ")} {instance.BundlePath}");
                }
            })
        };
        rootCommand.Add(listCommand);

        var msbuildCommand = new SubprocessCommand(i => i.MSBuildDllPath, "msbuild", "Invoke the MSBuild bundled with Visual Studio")
        {
            Kind = SubprocessKind.Mono
        };
        rootCommand.Add(msbuildCommand);

        var vstoolCommand = new SubprocessCommand(i => i.VSToolPath, "vstool", "Invoke the Visual Studio tool runner");
        rootCommand.Add(vstoolCommand);

        var pathCommand = new Command("path", "Print path to Visual Studio app bundle");
        pathCommand.Handler = new VSInstanceCommandHandler(GetInstance, i => { Console.WriteLine(i.BundlePath); return 0; });
        rootCommand.Add(pathCommand);

        var versionCommand = new Command("version", "Print version of Visual Studio");
        versionCommand.Handler = new VSInstanceCommandHandler(GetInstance, i => { Console.WriteLine(i.BundleVersion); return 0; });
        rootCommand.Add(versionCommand);

        //var openCommand = new Command("open", "Opens the specified files with Visual Studio");
        //rootCommand.Add(openCommand);

        var builder = new CommandLineBuilder(rootCommand);

        builder.UseMiddleware((c, n) => SubprocessCommand.Dispatch(c, n, GetInstance), MiddlewareOrder.ExceptionHandler);

        builder.UseDefaults();
        var parser = builder.Build();
        return parser.Invoke(args);
    }

    class VSInstanceCommandHandler : ICommandHandler
    {
        public VSInstanceCommandHandler(Func<ParseResult, VSMacInstance> getInstance, Func<VSMacInstance, int> handler)
        {
            this.getInstance = getInstance;
            this.handler = handler;
        }

        readonly Func<ParseResult, VSMacInstance> getInstance;
        readonly Func<VSMacInstance, int> handler;

        public Task<int> InvokeAsync(InvocationContext context)
        {
            var instance = getInstance(context.ParseResult);
            if (instance == null)
            {
                return Task.FromResult(1);
            }

            return Task.FromResult (handler(instance));
        }
    }
}