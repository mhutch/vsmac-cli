// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

sealed class SubprocessCommand<T>(Func<T, string, string?> processPathProvider, string name, string? description = null) : DispatchCommand<T>(name, description)
{
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

    public async override Task<int> InvokeAsync(T context, IEnumerable<string> args, CancellationToken token)
    {
        var processPath = processPathProvider(context, Name);

        if(processPath == null)
        {
            return 1;
        }

        var psi = CreateStartInfo(Kind, processPath);
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        var process = Process.Start(psi)!;
        await process.WaitForExitAsync(token);
        return process.ExitCode;
    }
}

enum SubprocessKind
{
    Mono,
    Native
}
