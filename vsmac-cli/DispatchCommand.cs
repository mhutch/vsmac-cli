// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Command that handles all arguments that come after it
/// </summary>
/// <typeparam name="T">Context </typeparam>
abstract class DispatchCommand<T> : Command
{
    public DispatchCommand(string name, string? description = null)
        : base(name, description)
    {
        TreatUnmatchedTokensAsErrors = false;
    }

    /// <param name="context">Context created from preceding arguments</param>
    /// <param name="args">The arguments that came after this command</param>
    public abstract Task<int> InvokeAsync(T context, IEnumerable<string> args, CancellationToken token);

    /// <summary>
    /// Registers dispatch command handler into middleware pipeline.
    /// </summary>
    /// <param name="dispatchContextProvider">Processes preceding arguments to create context for the dispatch</param>
    public static void RegisterMiddleware(CommandLineBuilder builder, Func<ParseResult, T?> dispatchContextProvider)
    {
        builder.UseMiddleware((c, n) => Dispatch(c, n, dispatchContextProvider), MiddlewareOrder.ExceptionHandler);
    }

    static async Task Dispatch(
        InvocationContext context, Func<InvocationContext, Task> next, Func<ParseResult, T?> dispatchContextProvider)
    {
        if (context.ParseResult.CommandResult.Command is not DispatchCommand<T> sub)
        {
            await next(context);
            return;
        }

        var dispatchContext = dispatchContextProvider(context.ParseResult);
        if (dispatchContext == null)
        {
            context.ExitCode = 1;
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

        var result = await sub.InvokeAsync(dispatchContext, subArgs, context.GetCancellationToken());
        context.ExitCode = result;
    }
}
