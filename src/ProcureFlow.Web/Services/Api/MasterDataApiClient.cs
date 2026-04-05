using System.Net;
using System.Net.Http.Json;
using ProcureFlow.Web.Endpoints.MasterData;

namespace ProcureFlow.Web.Services.Api;

public sealed class MasterDataApiClient
{
    private readonly HttpClient _httpClient;

    public MasterDataApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<CategoryFlatResponse> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => GetAsync<CategoryFlatResponse>("/api/master-data/categories", cancellationToken);

    public Task<CategoryTreeResponse> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
        => GetAsync<CategoryTreeResponse>("/api/master-data/categories/tree", cancellationToken);

    public Task<AdministrativeUnitsResponse> GetAdministrativeUnitsAsync(
        int? level = null,
        string? parentCode = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/master-data/admin-units?page={page}&pageSize={pageSize}";
        if (level.HasValue)
        {
            url += $"&level={level.Value}";
        }
        if (!string.IsNullOrWhiteSpace(parentCode))
        {
            url += $"&parentCode={Uri.EscapeDataString(parentCode)}";
        }

        return GetAsync<AdministrativeUnitsResponse>(url, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ApiError.FromResponseAsync(response, cancellationToken));
        }

        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken))
            ?? throw new ApiException(new ApiError(HttpStatusCode.InternalServerError, "EMPTY_RESPONSE", "Response payload was empty", null));
    }
}
