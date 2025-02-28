﻿using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// The JSON output formatter
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    private readonly IAnsiConsole _ansiConsole;

    /// <summary>
    /// Creates a new JSON output formatter with a default console
    /// </summary>
    public JsonOutputFormatter() : this(AnsiConsole.Console)
    {
    }

    /// <summary>
    /// Creates a new JSON output formatter with the provided console
    /// </summary>
    /// <param name="console">The console to use</param>
    public JsonOutputFormatter(IAnsiConsole console)
    {
        this._ansiConsole = console;
    }

    /// <inheritdoc />
    public async Task WriteOutputAsync(Stream? content, IOutputFormatterOptions? options = null, CancellationToken cancellationToken = default)
    {
        string resultStr;
        if (content == null || content == Stream.Null)
        {
            return;
        }

        if (options is IJsonOutputFormatterOptions jsonOptions && jsonOptions.OutputIndented)
        {
            using var result = await ProcessJsonAsync(content, jsonOptions.OutputIndented, cancellationToken);
            using var r = new StreamReader(result);
            resultStr = await r.ReadToEndAsync(cancellationToken);
        }
        else
        {
            using var reader = new StreamReader(content);
            resultStr = await reader.ReadToEndAsync(cancellationToken);
        }

        _ansiConsole.WriteLine(resultStr);
    }

    /// <summary>
    /// Given a JSON input stream, returns a processed JSON stream with optional indentation
    /// </summary>
    /// <param name="input">JSON input stream</param>
    /// <param name="indent">Whether to return indented output</param>
    /// <param name="cancellationToken">The cancellation token</param>
    private static async Task<Stream> ProcessJsonAsync(Stream input, bool indent = true, CancellationToken cancellationToken = default)
    {
        Stream cache = new MemoryStream();
        if (!input.CanSeek) {
            // copy the stream
            await input.CopyToAsync(cache, cancellationToken);
            cache.Position = 0;
        } else {
            cache = input;
        }

        try
        {
            var jsonDoc = await JsonDocument.ParseAsync(cache, default, cancellationToken);
            var outputStream = new MemoryStream();
            await JsonSerializer.SerializeAsync<object>(outputStream, jsonDoc, cancellationToken: cancellationToken, options: new() { WriteIndented = indent });
            outputStream.Position = 0;
            return outputStream;
        }
        catch (JsonException)
        {
        }

        cache.Position = 0;
        return cache;
    }
}
