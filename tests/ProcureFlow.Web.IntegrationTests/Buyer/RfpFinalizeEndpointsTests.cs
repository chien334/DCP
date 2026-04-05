using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProcureFlow.Web.IntegrationTests.Buyer;

public class RfpFinalizeEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public RfpFinalizeEndpointsTests(WebApplicationFactory<Program> factory)
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
            title = "Finalize RFP",
            description = "Finalize flow test",
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

    [Fact(DisplayName = "FIN-01 buyer can finalize winning bid")]
    public async Task FIN01_BuyerCanFinalizeWinningBid()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);
        var bidId = await InviteAndCreateBidAsync(buyer, vendor, rfpId, vendorCompanyId, rfpItemId);

        var finalizeResp = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/finalize", new
        {
            rfpBidId = bidId,
            note = "Selected as winner"
        });

        Assert.Equal(HttpStatusCode.Created, finalizeResp.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var finalize = await db.RfpFinalizes.Include(f => f.Items).SingleAsync(f => f.RfpId == rfpId);
        Assert.Equal(bidId, finalize.RfpBidId);
        Assert.Single(finalize.Items);

        var rfp = await db.Rfps.SingleAsync(r => r.Id == rfpId);
        Assert.Equal(RfpStatus.Closed, rfp.Status);
    }

    [Fact(DisplayName = "FIN-02 snapshot pricing is immutable after bid changes")]
    public async Task FIN02_SnapshotPricingIsImmutable()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);
        var bidId = await InviteAndCreateBidAsync(buyer, vendor, rfpId, vendorCompanyId, rfpItemId, unitPrice: 12000m);

        var finalizeResp = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/finalize", new { rfpBidId = bidId });
        finalizeResp.EnsureSuccessStatusCode();

        // Simulate later mutation on original bid item
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var bidItem = await db.RfpBidItems.FirstAsync(i => i.RfpBidId == bidId);
            bidItem.UnitPrice = 99999m;
            bidItem.TotalPrice = 999990m;
            await db.SaveChangesAsync();
        }

        var getFinalize = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}/finalize");
        Assert.Equal(HttpStatusCode.OK, getFinalize.StatusCode);

        var body = await getFinalize.Content.ReadFromJsonAsync<JsonElement>();
        var item = body.GetProperty("items")[0];
        Assert.Equal(12000m, item.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(120000m, item.GetProperty("totalPrice").GetDecimal());
    }

    [Fact(DisplayName = "FIN-01 duplicate finalize returns 409")]
    public async Task FIN01_DuplicateFinalizeReturns409()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);
        var bidId = await InviteAndCreateBidAsync(buyer, vendor, rfpId, vendorCompanyId, rfpItemId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/finalize", new { rfpBidId = bidId });
        var second = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/finalize", new { rfpBidId = bidId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact(DisplayName = "FIN-01 invalid bid for rfp returns 422")]
    public async Task FIN01_InvalidBidForRfpReturns422()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);
        _ = await InviteAndCreateBidAsync(buyer, vendor, rfpId, vendorCompanyId, rfpItemId);

        var invalidResp = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/finalize", new { rfpBidId = 999999 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidResp.StatusCode);
    }

    [Fact(DisplayName = "FIN-01 only submitted bid can be finalized")]
    public async Task FIN01_OnlySubmittedBidCanBeFinalized()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);
        var bidId = await InviteAndCreateBidAsync(buyer, vendor, rfpId, vendorCompanyId, rfpItemId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var bid = await db.RfpBids.FirstAsync(b => b.Id == bidId);
            bid.Status = RfpBidStatus.Withdrawn;
            await db.SaveChangesAsync();
        }

        var response = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/finalize", new { rfpBidId = bidId });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "RBAC vendor cannot finalize buyer rfp")]
    public async Task RBAC_VendorCannotFinalizeBuyerRfp()
    {
        var vendor = CreateVendorClient();
        var response = await vendor.PostAsJsonAsync("/api/buyer/rfps/1/finalize", new { rfpBidId = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
