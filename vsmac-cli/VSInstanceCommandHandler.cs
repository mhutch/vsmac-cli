// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

using VSMacLocator;

sealed class VSInstanceCommandHandler(Func<ParseResult, VSMacInstance?> getInstance, Func<VSMacInstance, int> handler) : ICommandHandler
{
    public Task<int> InvokeAsync(InvocationContext context) => Task.FromResult (Invoke (context));

    public int Invoke(InvocationContext context) =>
        getInstance(context.ParseResult) is VSMacInstance instance? handler(instance) : 1;
}
