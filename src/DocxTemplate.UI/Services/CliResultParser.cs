using System;
using System.Text.Json;

namespace DocxTemplate.UI.Services;

public class CliResultParser
{
    public T ParseJsonResult<T>(string jsonOutput) where T : class
    {
        if (string.IsNullOrEmpty(jsonOutput))
        {
            throw new ArgumentException("JSON output cannot be null or empty", nameof(jsonOutput));
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            return JsonSerializer.Deserialize<T>(jsonOutput, options)
                ?? throw new InvalidOperationException("Failed to deserialize JSON response");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse CLI JSON response: {ex.Message}", ex);
        }
    }

    public bool IsSuccessResponse(string jsonOutput)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonOutput);
            return document.RootElement.TryGetProperty("success", out var successElement) 
                && successElement.GetBoolean();
        }
        catch
        {
            return false;
        }
    }
}