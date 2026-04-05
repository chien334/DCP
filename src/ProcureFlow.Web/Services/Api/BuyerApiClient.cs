using System.Net;
using System.Net.Http.Json;
using ProcureFlow.Web.Endpoints.Buyer;
using ProcureFlow.Web.Endpoints.Vendor;

namespace ProcureFlow.Web.Services.Api;

public sealed class BuyerApiClient
{
    private readonly HttpClient _httpClient;

    public BuyerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RfpListResponse> GetRfpsAsync(
        int? companyId = null,
        int? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/buyer/rfps?page={page}&pageSize={pageSize}";
        if (companyId.HasValue)
        {
            url += $"&companyId={companyId.Value}";
        }
        if (status.HasValue)
        {
            url += $"&status={status.Value}";
        }

        return await GetAsync<RfpListResponse>(url, cancellationToken);
    }

    public Task<RfpDetailResponse> GetRfpDetailAsync(int rfpId, CancellationToken cancellationToken = default)
        => GetAsync<RfpDetailResponse>($"/api/buyer/rfps/{rfpId}", cancellationToken);

    public Task<VendorInviteDto[]> GetRfpInvitesAsync(int rfpId, CancellationToken cancellationToken = default)
        => GetAsync<VendorInviteDto[]>($"/api/buyer/rfps/{rfpId}/invites", cancellationToken);

    public Task<BidListItem[]> GetRfpBidsAsync(int rfpId, CancellationToken cancellationToken = default)
        => GetAsync<BidListItem[]>($"/api/buyer/rfps/{rfpId}/bids", cancellationToken);

    public Task<FinalizeDetailResponse> GetFinalizeAsync(int rfpId, CancellationToken cancellationToken = default)
        => GetAsync<FinalizeDetailResponse>($"/api/buyer/rfps/{rfpId}/finalize", cancellationToken);

    public Task<ContractDetailResponse> GetContractByRfpAsync(int rfpId, CancellationToken cancellationToken = default)
        => GetAsync<ContractDetailResponse>($"/api/buyer/rfps/{rfpId}/contract", cancellationToken);

    public Task<ContractDetailResponse> GetContractAsync(int contractId, CancellationToken cancellationToken = default)
        => GetAsync<ContractDetailResponse>($"/api/buyer/contracts/{contractId}", cancellationToken);

    public Task<IdResult> CreateRfpAsync(CreateRfpRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateRfpRequest, IdResult>("/api/buyer/rfps", request, cancellationToken, HttpStatusCode.Created);

    public Task<IdResult> InviteVendorAsync(int rfpId, InviteVendorRequest request, CancellationToken cancellationToken = default)
        => PostAsync<InviteVendorRequest, IdResult>($"/api/buyer/rfps/{rfpId}/invites", request, cancellationToken, HttpStatusCode.Created);

    public Task<IdResult> FinalizeBidAsync(int rfpId, FinalizeBidRequest request, CancellationToken cancellationToken = default)
        => PostAsync<FinalizeBidRequest, IdResult>($"/api/buyer/rfps/{rfpId}/finalize", request, cancellationToken, HttpStatusCode.Created);

    public Task<IdResult> CreateContractAsync(int rfpId, CreateContractRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateContractRequest, IdResult>($"/api/buyer/rfps/{rfpId}/contract", request, cancellationToken, HttpStatusCode.Created);

    public Task<ContractDetailResponse> SignContractAsync(int contractId, BuyerSignContractRequest request, CancellationToken cancellationToken = default)
        => PostAsync<BuyerSignContractRequest, ContractDetailResponse>($"/api/buyer/contracts/{contractId}/sign", request, cancellationToken, HttpStatusCode.OK);

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

public sealed record IdResult(int Id);
