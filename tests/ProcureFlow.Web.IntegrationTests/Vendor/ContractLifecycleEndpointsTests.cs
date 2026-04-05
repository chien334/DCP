using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProcureFlow.Web.IntegrationTests.Vendor;

public class ContractLifecycleEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public ContractLifecycleEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _databaseName = Guid.NewGuid().ToString();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                services.Remove(descriptor);
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        });
    }

    private HttpClient CreateAdminClient() => CreateClient("Admin", "admin-01");
    private HttpClient CreateBuyerClient() => CreateClient("Buyer", "buyer-01");
    private HttpClient CreateVendorClient() => CreateClient("Vendor", "vendor-01");

    private HttpClient CreateClient(string role, string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Role", role);
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        return client;
    }

    private async Task<int> SeedCompanyAsync(HttpClient admin, string suffix)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = $"Company {suffix}",
            shortName = $"C{suffix}",
            taxCode = $"TAX{Guid.NewGuid():N}"[..20],
            email = $"{suffix.ToLower()}@test.com",
            phone = "0901234567",
            status = 1,
            address = new { country = "VN", province = "HCM", district = "Q1", ward = "BN", addressLine = "1 St", postalCode = "700000" }
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<(int rfpId, int rfpItemId)> SeedRfpWithItemAsync(HttpClient buyer, int companyId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cat = new Category
        {
            CategoryCode = $"CAT-{Guid.NewGuid():N}"[..20],
            CategoryName = "Test",
            TreePath = "/test",
            IsActive = true,
            DisplayOrder = 1,
            CreateAt = DateTime.UtcNow
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var createRfp = await buyer.PostAsJsonAsync("/api/buyer/rfps", new
        {
            companyId,
            title = "Contract RFP",
            description = "Contract flow test",
            categoryId = cat.Id,
            type = 1,
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    name = "Laptop",
                    quantity = 10,
                    unit = "pcs",
                    note = (string?)null,
                    specs = new[]
                    {
                        new { key = "RAM", valueText = "16GB", valueNumber = (decimal?)16, valueBoolean = (bool?)null, unit = "GB" }
                    }
                }
            }
        });
        createRfp.EnsureSuccessStatusCode();

        var createBody = await createRfp.Content.ReadFromJsonAsync<JsonElement>();
        var rfpId = createBody.GetProperty("id").GetInt32();

        var detailResp = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}");
        var detail = await detailResp.Content.ReadFromJsonAsync<JsonElement>();
        var rfpItemId = detail.GetProperty("items")[0].GetProperty("id").GetInt32();

        return (rfpId, rfpItemId);
    }

    private async Task<int> InviteAndCreateBidAsync(
        HttpClient buyer,
        HttpClient vendor,
        int rfpId,
        int vendorCompanyId,
        int rfpItemId,
        decimal unitPrice = 10000m)
    {
        var inviteResp = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites", new { companyId = vendorCompanyId });
        inviteResp.EnsureSuccessStatusCode();

        var bidResp = await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = unitPrice * 10,
            grandTotal = unitPrice * 10 * 1.1m,
            currency = "VND",
            proposal = "Best offer",
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId,
                    brand = "Dell",
                    quantity = 10,
                    unitPrice,
                    totalPrice = unitPrice * 10,
                    note = (string?)null,
                    specs = new[]
                    {
                        new { key = "RAM", valueText = "16GB", valueNumber = (decimal?)16, valueBoolean = (bool?)null, unit = "GB" }
                    }
                }
            }
        });
        bidResp.EnsureSuccessStatusCode();

        var bidBody = await bidResp.Content.ReadFromJsonAsync<JsonElement>();
        return bidBody.GetProperty("id").GetInt32();
    }

    private async Task<(int rfpId, int vendorCompanyId)> SeedFinalizedRfpAsync()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);
        var bidId = await InviteAndCreateBidAsync(buyer, vendor, rfpId, vendorCompanyId, rfpItemId);

        var finalizeResp = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/finalize", new { rfpBidId = bidId });
        finalizeResp.EnsureSuccessStatusCode();

        return (rfpId, vendorCompanyId);
    }

    [Fact(DisplayName = "FIN-03 buyer can create contract from finalized bid")]
    public async Task FIN03_BuyerCanCreateContractFromFinalize()
    {
        var buyer = CreateBuyerClient();
        var (rfpId, _) = await SeedFinalizedRfpAsync();

        var create = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new
        {
            contractNo = "CT-0001",
            title = "Purchase Contract",
            fileUrl = "/contracts/ct-0001.pdf",
            note = "Initial contract"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contract = await db.RfpContracts.Include(c => c.RfpFinalize).SingleAsync();
        Assert.Equal(rfpId, contract.RfpFinalize.RfpId);
        Assert.Equal(RfpContractStatus.Created, contract.Status);
    }

    [Fact(DisplayName = "FIN-03 duplicate contract creation returns 409")]
    public async Task FIN03_DuplicateContractCreationReturns409()
    {
        var buyer = CreateBuyerClient();
        var (rfpId, _) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0002" });
        var second = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0003" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact(DisplayName = "FIN-04 buyer can sign contract")]
    public async Task FIN04_BuyerCanSignContract()
    {
        var buyer = CreateBuyerClient();
        var (rfpId, _) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0004" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contractId = await db.RfpContracts.Select(c => c.Id).SingleAsync();

        var signResp = await buyer.PostAsJsonAsync($"/api/buyer/contracts/{contractId}/sign", new { note = "Buyer approved" });
        Assert.Equal(HttpStatusCode.OK, signResp.StatusCode);

        var contract = await db.RfpContracts.SingleAsync(c => c.Id == contractId);
        Assert.True(contract.BuyerSign);
        Assert.NotNull(contract.BuyerSignAt);
        Assert.Equal(RfpContractStatus.PartiallySigned, contract.Status);
    }

    [Fact(DisplayName = "FIN-05 vendor can sign contract and move to signed status")]
    public async Task FIN05_VendorCanSignContract()
    {
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();
        var (rfpId, vendorCompanyId) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0005" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contractId = await db.RfpContracts.Select(c => c.Id).SingleAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/contracts/{contractId}/sign", new { note = "Buyer approved" });
        var signResp = await vendor.PostAsJsonAsync($"/api/vendor/contracts/{contractId}/sign", new { companyId = vendorCompanyId, note = "Vendor approved" });

        Assert.Equal(HttpStatusCode.OK, signResp.StatusCode);

        var contract = await db.RfpContracts.SingleAsync(c => c.Id == contractId);
        Assert.True(contract.VendorSign);
        Assert.NotNull(contract.VendorSignAt);
        Assert.Equal(RfpContractStatus.Signed, contract.Status);
    }

    [Fact(DisplayName = "FIN-05 vendor can decline contract")]
    public async Task FIN05_VendorCanDeclineContract()
    {
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();
        var (rfpId, vendorCompanyId) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0006" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contractId = await db.RfpContracts.Select(c => c.Id).SingleAsync();

        var declineResp = await vendor.PostAsJsonAsync($"/api/vendor/contracts/{contractId}/decline", new { companyId = vendorCompanyId, note = "Cannot meet terms" });

        Assert.Equal(HttpStatusCode.OK, declineResp.StatusCode);

        var contract = await db.RfpContracts.SingleAsync(c => c.Id == contractId);
        Assert.Equal(RfpContractStatus.Declined, contract.Status);
        Assert.Equal("Cannot meet terms", contract.Note);
    }

    [Fact(DisplayName = "FIN-05 vendor from another company cannot sign contract")]
    public async Task FIN05_VendorFromAnotherCompanyCannotSignContract()
    {
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();
        var (rfpId, _) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0007" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contractId = await db.RfpContracts.Select(c => c.Id).SingleAsync();

        var response = await vendor.PostAsJsonAsync($"/api/vendor/contracts/{contractId}/sign", new { companyId = 999999, note = "Invalid company" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "FIN-03 buyer can load contract by RFP id")]
    public async Task FIN03_BuyerCanLoadContractByRfpId()
    {
        var buyer = CreateBuyerClient();
        var (rfpId, _) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0008A" });

        var response = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}/contract");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(rfpId, body.GetProperty("rfpId").GetInt32());
    }

    [Fact(DisplayName = "FIN-03 buyer can load contract by contractId")]
    public async Task FIN03_BuyerCanLoadContractByContractId()
    {
        var buyer = CreateBuyerClient();
        var (rfpId, _) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0008" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contractId = await db.RfpContracts.Select(c => c.Id).SingleAsync();

        var response = await buyer.GetAsync($"/api/buyer/contracts/{contractId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(contractId, body.GetProperty("id").GetInt32());
        Assert.Equal(rfpId, body.GetProperty("rfpId").GetInt32());
    }

    [Fact(DisplayName = "FIN-04 repeated buyer sign returns 409 with buyer-signed code")]
    public async Task FIN04_RepeatedBuyerSignReturns409()
    {
        var buyer = CreateBuyerClient();
        var (rfpId, _) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0008B" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contractId = await db.RfpContracts.Select(c => c.Id).SingleAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/contracts/{contractId}/sign", new { note = "First buyer sign" });
        var second = await buyer.PostAsJsonAsync($"/api/buyer/contracts/{contractId}/sign", new { note = "Second buyer sign" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BUYER_ALREADY_SIGNED", body.GetProperty("code").GetString());
    }

    [Fact(DisplayName = "FIN-05 vendor can load contract detail endpoint")]
    public async Task FIN05_VendorCanLoadContractDetail()
    {
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();
        var (rfpId, vendorCompanyId) = await SeedFinalizedRfpAsync();

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/contract", new { contractNo = "CT-0009" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contractId = await db.RfpContracts.Select(c => c.Id).SingleAsync();

        var response = await vendor.GetAsync($"/api/vendor/contracts/{contractId}?companyId={vendorCompanyId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(contractId, body.GetProperty("id").GetInt32());
        Assert.Equal(rfpId, body.GetProperty("rfpId").GetInt32());
    }

    [Fact(DisplayName = "FIN-01 finalized bid cannot be updated returns 409")]
    public async Task FIN01_FinalizedBidCannotBeUpdated_Returns409()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);
        var bidId = await InviteAndCreateBidAsync(buyer, vendor, rfpId, vendorCompanyId, rfpItemId);

        var finalizeResp = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/finalize", new { rfpBidId = bidId });
        Assert.Equal(HttpStatusCode.Created, finalizeResp.StatusCode);

        var updateResp = await vendor.PutAsJsonAsync($"/api/vendor/bids/{bidId}", new
        {
            companyId = vendorCompanyId,
            vatRate = 12.0m,
            subTotal = 120000m,
            grandTotal = 134400m,
            currency = "VND",
            proposal = "Late update",
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId,
                    brand = "Acer",
                    quantity = 10,
                    unitPrice = 12000m,
                    totalPrice = 120000m,
                    note = "Late",
                    specs = Array.Empty<object>()
                }
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, updateResp.StatusCode);
    }
}
