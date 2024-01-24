// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

using VSMacLocator;

sealed class OpenCommandHandler(string name, string? description = null) : DispatchCommand<VSMacInstance>(name, description)
{
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