// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xping.Session.Serialization;

/// <summary>
/// Provides centralized JSON serialization configuration for the Xping SDK.
/// This class defines standard serialization options used across different components
/// to ensure consistency in how test execution data is serialized.
/// </summary>
public static class XpingSerializerOptions
{
    /// <summary>
    /// Gets JSON serialization options optimized for file persistence.
    /// Similar to API options but may include indentation for debugging.
    /// </summary>
    /// <remarks>
    /// Options:
    /// - PropertyNamingPolicy: CamelCase
    /// - WriteIndented: false (compact to save disk space)
    /// - DefaultIgnoreCondition: WhenWritingNull
    /// - PropertyNameCaseInsensitive: true
    /// - Converters: JsonStringEnumConverter
    /// </remarks>
    public static JsonSerializerOptions FileOptions { get; } = CreateFileOptions();

    /// <summary>
    /// Gets JSON serialization options for testing purposes.
    /// Includes indentation for better readability in test output.
    /// </summary>
    /// <remarks>
    /// Options:
    /// - PropertyNamingPolicy: CamelCase
    /// - WriteIndented: true (human-readable for debugging)
    /// - DefaultIgnoreCondition: WhenWritingNull
    /// - PropertyNameCaseInsensitive: true
    /// - Converters: JsonStringEnumConverter
    /// </remarks>
    public static JsonSerializerOptions TestOptions { get; } = CreateTestOptions();

    private static JsonSerializerOptions CreateFileOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }

    private static JsonSerializerOptions CreateTestOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
