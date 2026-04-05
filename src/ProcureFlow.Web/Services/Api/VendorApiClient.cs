using System.Net;
using System.Net.Http.Json;
using ProcureFlow.Web.Endpoints.Vendor;

namespace ProcureFlow.Web.Services.Api;

public sealed class VendorApiClient
{
    private readonly HttpClient _httpClient;

    public VendorApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<VendorInviteListResponse> GetInvitesAsync(
        int companyId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<VendorInviteListResponse>($"/api/vendor/invites?companyId={companyId}&page={page}&pageSize={pageSize}", cancellationToken);
    }

    public Task<VendorRfpDetailResponse> GetRfpAsync(int rfpId, int companyId, CancellationToken cancellationToken = default)
        => GetAsync<VendorRfpDetailResponse>($"/api/vendor/rfps/{rfpId}?companyId={companyId}", cancellationToken);

    public Task<BidListResponse> GetBidsAsync(
        int companyId,
        int? rfpId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/vendor/bids?page={page}&pageSize={pageSize}";
        url += $"&companyId={companyId}";
        if (rfpId.HasValue)
        {
            url += $"&rfpId={rfpId.Value}";
        }

        return GetAsync<BidListResponse>(url, cancellationToken);
    }

    public Task<BidDetailResponse> GetBidAsync(int bidId, int companyId, CancellationToken cancellationToken = default)
        => GetAsync<BidDetailResponse>($"/api/vendor/bids/{bidId}?companyId={companyId}", cancellationToken);

    public Task<VendorContractListResponse> GetContractsAsync(
        int companyId,
        int? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/vendor/contracts?companyId={companyId}&page={page}&pageSize={pageSize}";
        if (status.HasValue)
        {
            url += $"&status={status.Value}";
        }

        return GetAsync<VendorContractListResponse>(url, cancellationToken);
    }

    public Task<VendorContractDetailResponse> GetContractAsync(int contractId, int companyId, CancellationToken cancellationToken = default)
        => GetAsync<VendorContractDetailResponse>($"/api/vendor/contracts/{contractId}?companyId={companyId}", cancellationToken);

    public Task<IdResult> CreateBidAsync(CreateBidRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateBidRequest, IdResult>("/api/vendor/bids", request, cancellationToken, HttpStatusCode.Created);

    public Task<IdResult> UpdateBidAsync(int bidId, UpdateBidRequest request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateBidRequest, IdResult>($"/api/vendor/bids/{bidId}", request, cancellationToken);

    public Task<IdResult> SignContractAsync(int contractId, VendorActionRequest request, CancellationToken cancellationToken = default)
        => PostAsync<VendorActionRequest, IdResult>($"/api/vendor/contracts/{contractId}/sign", request, cancellationToken, HttpStatusCode.OK);

    public Task<IdResult> DeclineContractAsync(int contractId, VendorActionRequest request, CancellationToken cancellationToken = default)
        => PostAsync<VendorActionRequest, IdResult>($"/api/vendor/contracts/{contractId}/decline", request, cancellationToken, HttpStatusCode.OK);

    private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken))
            ?? throw new ApiException(new ApiError(HttpStatusCode.InternalServerError, "EMPTY_RESPONSE", "Response payload was empty", null));
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken cancellationToken,
        HttpStatusCode expectedStatus)
    {
        using var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken, expectedStatus);
        return (await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken))
            ?? throw new ApiException(new ApiError(HttpStatusCode.InternalServerError, "EMPTY_RESPONSE", "Response payload was empty", null));
    }

    private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(url, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken))
            ?? throw new ApiException(new ApiError(HttpStatusCode.InternalServerError, "EMPTY_RESPONSE", "Response payload was empty", null));
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken, HttpStatusCode? expected = null)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ApiError.FromResponseAsync(response, cancellationToken));
        }

        if (expected.HasValue && response.StatusCode != expected.Value)
        {
            throw new ApiException(new ApiError(response.StatusCode, "UNEXPECTED_STATUS", $"Expected {(int)expected.Value} but got {(int)response.StatusCode}", null));
        }
    }
}
