// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        var msbuildCommand = new SubprocessCommand<VSMacInstance>(FindTool, "msbuild", "Invoke the MSBuild bundled with Visual Studio")
        {
            Kind = SubprocessKind.Mono
        };
        rootCommand.Add(msbuildCommand);

        var vstoolCommand = new SubprocessCommand<VSMacInstance>(FindTool, "vstool", "Invoke the Visual Studio tool runner");
        rootCommand.Add(vstoolCommand);

        var pathCommand = new Command("path", "Print path to Visual Studio app bundle");
        pathCommand.Handler = new VSInstanceCommandHandler(GetInstance, i => { Console.WriteLine(i.BundlePath); return 0; });
        rootCommand.Add(pathCommand);

        var versionCommand = new Command("version", "Print version of Visual Studio");
        versionCommand.Handler = new VSInstanceCommandHandler(GetInstance, i => { Console.WriteLine(i.BundleVersion); return 0; });
        rootCommand.Add(versionCommand);

        var openCommand = new OpenCommandHandler("open", "Opens the specified files with Visual Studio");
        rootCommand.Add(openCommand);

        var builder = new CommandLineBuilder(rootCommand);

        SubprocessCommand<VSMacInstance>.RegisterMiddleware(builder, GetInstance);

        builder.UseDefaults();
        var parser = builder.Build();
        return parser.Invoke(args);
    }

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

    class VSInstanceCommandHandler : ICommandHandler
    {
        public VSInstanceCommandHandler(Func<ParseResult, VSMacInstance?> getInstance, Func<VSMacInstance, int> handler)
        {
            this.getInstance = getInstance;
            this.handler = handler;
        }

        readonly Func<ParseResult, VSMacInstance?> getInstance;
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

    class OpenCommandHandler : DispatchCommand<VSMacInstance>
    {
        public OpenCommandHandler(string name, string? description = null) : base(name, description)
        {
        }

        public override async Task<int> InvokeAsync(VSMacInstance context, IEnumerable<string> args, CancellationToken token)
        {
            var sb = new StringBuilder("-a");
            void AppendEscaped(string s)
            {
                if (s.IndexOf('\\') > -1)
                {
                    s = s.Replace("\\", "\\\\");
                }
                if (s.IndexOf('"') > -1)
                {
                    s = s.Replace("\"", "\\\"");
                }

                sb.Append(' ');
                sb.Append('"');
                sb.Append(s);
                sb.Append('"');
            }

            AppendEscaped(context.BundlePath);
            foreach (var a in args)
            {
                AppendEscaped(a);
            }

            Console.WriteLine(sb.ToString());

            var psi = new ProcessStartInfo("open", sb.ToString())
            {
                UseShellExecute = false
            };

            var process = Process.Start(psi)!;
            await process.WaitForExitAsync(token);
            return process.ExitCode;
        }
    }
}