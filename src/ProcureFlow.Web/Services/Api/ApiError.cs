using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProcureFlow.Web.Services.Api;

public sealed record ApiError(HttpStatusCode StatusCode, string? Code, string Message, IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static async Task<ApiError> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var message = response.ReasonPhrase ?? "API request failed";
        string? code = null;
        IReadOnlyDictionary<string, string[]>? validationErrors = null;

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

            if (payload.TryGetProperty("code", out var codeProperty) && codeProperty.ValueKind == JsonValueKind.String)
            {
                code = codeProperty.GetString();
            }

            if (payload.TryGetProperty("title", out var titleProperty) && titleProperty.ValueKind == JsonValueKind.String)
            {
                message = titleProperty.GetString() ?? message;
            }
            else if (payload.TryGetProperty("message", out var messageProperty) && messageProperty.ValueKind == JsonValueKind.String)
            {
                message = messageProperty.GetString() ?? message;
            }

            if (payload.TryGetProperty("errors", out var errorsProperty) && errorsProperty.ValueKind == JsonValueKind.Object)
            {
                var parsed = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in errorsProperty.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        parsed[property.Name] = property.Value
                            .EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString() ?? string.Empty)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray();
                    }
                }

                if (parsed.Count > 0)
                {
                    validationErrors = parsed;
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        code = "VALIDATION_ERROR";
                    }
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        message = "Validation failed";
                    }
                }
            }
        }
        catch
        {
            // Keep fallback message when parsing non-JSON responses.
        }

        return new ApiError(response.StatusCode, code, message, validationErrors);
    }
}

public sealed class ApiException : Exception
{
    public ApiException(ApiError error)
        : base(error.Message)
    {
        Error = error;
    }

    public ApiError Error { get; }
}
