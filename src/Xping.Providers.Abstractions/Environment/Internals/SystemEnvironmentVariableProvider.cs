// SPDX-License-Identifier: MIT
// Copyright (c) Xping

namespace Xping.Providers.Environment.Internals;

/// <summary>
/// Default implementation of <see cref="IEnvironmentVariableProvider"/> that reads from
/// the current process environment via <see cref="System.Environment.GetEnvironmentVariable(string)"/>.
/// </summary>
internal sealed class SystemEnvironmentVariableProvider : IEnvironmentVariableProvider
{
    /// <inheritdoc/>
    public string? GetVariable(string name)
    {
        try
        {
            return System.Environment.GetEnvironmentVariable(name);
        }
        catch
        {
            return null;
        }
    }
}
