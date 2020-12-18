// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VSMacLocator;

class SubprocessCommand : Command
{
    public SubprocessCommand(Func<VSMacInstance, string> processPathProvider, string name, string description = null)
        : base(name, description)
    {
        TreatUnmatchedTokensAsErrors = false;
        ProcessPathProvider = processPathProvider;
    }

    public Func<VSMacInstance, string> ProcessPathProvider { get; }
    public SubprocessKind Kind { get; set; } = SubprocessKind.Native;

    static ProcessStartInfo CreateStartInfo(SubprocessKind kind, string path)
    {
        ProcessStartInfo psi;
        switch (kind)
        {
            case SubprocessKind.Mono:
                psi = new ProcessStartInfo("mono");
                psi.ArgumentList.Add(path);
                break;
            case SubprocessKind.Native:
                psi = new ProcessStartInfo(path);
                break;
            default:
                throw new ArgumentException($"Unknown value {kind}", nameof(kind));
        }
        psi.UseShellExecute = false;
        return psi;
    }

    public async Task<int> InvokeAsync(VSMacInstance instance, IEnumerable<string> args, CancellationToken token)
    {
        var processPath = ProcessPathProvider(instance);

        if(processPath == null || !File.Exists(processPath))
        {
            Console.Error.WriteLine($"Did not find '{Name}' in Visual Studio {instance.BundleVersion}");
            return 1;
        }

        var psi = CreateStartInfo(Kind, processPath);
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        var process = System.Diagnostics.Process.Start(psi);

        await process.WaitForExitAsync(token);
        return process.ExitCode;
    }

    public static async Task Dispatch(
        InvocationContext context, Func<InvocationContext, Task> next, Func<ParseResult, VSMacInstance> getInstance)
    {
        if (context.ParseResult.CommandResult.Command is not SubprocessCommand sub)
        {
            await next(context);
            return;
        }

        var instance = getInstance(context.ParseResult);
        if (instance == null)
        {
            context.ResultCode = 1;
            return;
        }

        IEnumerable<string> RecoverHelpAndVersionTokens()
        {
            foreach (var c in context.ParseResult.Tokens)
            {
                if (c.Type == TokenType.Option)
                {
                    switch (c.Value)
                    {
                        case "-h":
                        case "/h":
                        case "--help":
                        case "--version":
                            yield return c.Value;
                            break;
                    }
                }
            }
        }

        IEnumerable<string> subArgs =
            RecoverHelpAndVersionTokens()
            .Concat(context.ParseResult.UnmatchedTokens)
            .Concat(context.ParseResult.UnparsedTokens);

        var result = await sub.InvokeAsync(instance, subArgs, context.GetCancellationToken());
        context.ResultCode = result;
    }
}

enum SubprocessKind
{
    Mono,
    Native
}
