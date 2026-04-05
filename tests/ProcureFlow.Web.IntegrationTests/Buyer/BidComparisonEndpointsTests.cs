using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProcureFlow.Web.IntegrationTests.Buyer;

public class BidComparisonEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public BidComparisonEndpointsTests(WebApplicationFactory<Program> factory)
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
    private HttpClient CreateVendorClient(string userId = "vendor-01") => CreateClient("Vendor", userId);

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

    private async Task<(int rfpId, int rfpItemId1, int rfpItemId2)> SeedRfpWith2ItemsAsync(HttpClient buyer, int companyId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cat = new ProcureFlow.Core.Entities.Category
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

        var response = await buyer.PostAsJsonAsync("/api/buyer/rfps", new
        {
            companyId,
            title = "Comparison Test RFP",
            description = "Test",
            categoryId = cat.Id,
            type = 1,
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    name = "Laptop",
                    quantity = 10,
                    unit = "cái",
                    note = (string?)null,
                    specs = new[]
                    {
                        new { key = "RAM", valueText = "16GB", valueNumber = (decimal?)16, valueBoolean = (bool?)null, unit = "GB" },
                        new { key = "SSD", valueText = "512GB", valueNumber = (decimal?)512, valueBoolean = (bool?)null, unit = "GB" }
                    }
                },
                new
                {
                    name = "Monitor",
                    quantity = 10,
                    unit = "cái",
                    note = "24 inch preferred",
                    specs = new[]
                    {
                        new { key = "Size", valueText = "24\"", valueNumber = (decimal?)24, valueBoolean = (bool?)null, unit = "inch" }
                    }
                }
            }
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rfpId = body.GetProperty("id").GetInt32();

        var detailResp = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}");
        var detail = await detailResp.Content.ReadFromJsonAsync<JsonElement>();
        var items = detail.GetProperty("items");
        var rfpItemId1 = items[0].GetProperty("id").GetInt32();
        var rfpItemId2 = items[1].GetProperty("id").GetInt32();

        return (rfpId, rfpItemId1, rfpItemId2);
    }

    private async Task InviteAndBidAsync(
        HttpClient buyer, HttpClient vendor,
        int rfpId, int vendorCompanyId, int rfpItemId1, int rfpItemId2,
        string brand1, decimal unitPrice1, string brand2, decimal unitPrice2)
    {
        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = unitPrice1 * 10 + unitPrice2 * 10,
            grandTotal = (unitPrice1 * 10 + unitPrice2 * 10) * 1.1m,
            currency = "VND",
            privacyMode = 1,
            items = new object[]
            {
                new
                {
                    rfpItemId = rfpItemId1,
                    brand = brand1,
                    quantity = 10,
                    unitPrice = unitPrice1,
                    totalPrice = unitPrice1 * 10,
                    specs = new[]
                    {
                        new { key = "RAM", valueText = "16GB", valueNumber = (decimal?)16, unit = "GB", valueBoolean = (bool?)null },
                        new { key = "SSD", valueText = "512GB", valueNumber = (decimal?)512, unit = "GB", valueBoolean = (bool?)null }
                    }
                },
                new
                {
                    rfpItemId = rfpItemId2,
                    brand = brand2,
                    quantity = 10,
                    unitPrice = unitPrice2,
                    totalPrice = unitPrice2 * 10,
                    specs = new[]
                    {
                        new { key = "Size", valueText = "24\"", valueNumber = (decimal?)24, unit = "inch", valueBoolean = (bool?)null }
                    }
                }
            }
        });
    }

    // ── BID-05: Comparison returns RFP items with per-vendor bids ────────────────

    [Fact(DisplayName = "BID-05 comparison returns structured item-level vendor matrix")]
    public async Task BID05_ComparisonReturnsItemLevelMatrix()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor1 = CreateVendorClient("vendor-01");
        var vendor2 = CreateVendorClient("vendor-02");

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId1 = await SeedCompanyAsync(admin, "VendorA");
        var vendorCompanyId2 = await SeedCompanyAsync(admin, "VendorB");
        var (rfpId, itemId1, itemId2) = await SeedRfpWith2ItemsAsync(buyer, buyerCompanyId);

        await InviteAndBidAsync(buyer, vendor1, rfpId, vendorCompanyId1, itemId1, itemId2,
            "Dell", 15000m, "LG", 5000m);
        await InviteAndBidAsync(buyer, vendor2, rfpId, vendorCompanyId2, itemId1, itemId2,
            "HP", 14000m, "Samsung", 4500m);

        var response = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}/comparison");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Verify structure
        Assert.Equal(rfpId, body.GetProperty("rfpId").GetInt32());
        Assert.Equal("Comparison Test RFP", body.GetProperty("rfpTitle").GetString());

        // 2 vendors
        var vendors = body.GetProperty("vendors");
        Assert.Equal(2, vendors.GetArrayLength());

        // 2 RFP items
        var items = body.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());

        // First item (Laptop) — both vendors bid
        var laptopItem = items[0];
        Assert.Equal("Laptop", laptopItem.GetProperty("name").GetString());
        Assert.Equal(10, laptopItem.GetProperty("quantity").GetInt32());

        // Required specs for Laptop
        var requiredSpecs = laptopItem.GetProperty("requiredSpecs");
        Assert.Equal(2, requiredSpecs.GetArrayLength());

        // Both vendors have bids for this item
        var laptopBids = laptopItem.GetProperty("bids");
        Assert.Equal(2, laptopBids.GetArrayLength());

        // Verify first vendor bid has brand
        var bid1Brand = laptopBids[0].GetProperty("brand").GetString();
        var bid2Brand = laptopBids[1].GetProperty("brand").GetString();
        Assert.Contains("Dell", new[] { bid1Brand, bid2Brand });
        Assert.Contains("HP", new[] { bid1Brand, bid2Brand });

        // Second item (Monitor) — both vendors bid
        var monitorItem = items[1];
        Assert.Equal("Monitor", monitorItem.GetProperty("name").GetString());
        var monitorBids = monitorItem.GetProperty("bids");
        Assert.Equal(2, monitorBids.GetArrayLength());
    }

    [Fact(DisplayName = "BID-05 comparison with partial bids shows null for missing items")]
    public async Task BID05_PartialBidsShowNullForMissingItems()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, itemId1, itemId2) = await SeedRfpWith2ItemsAsync(buyer, buyerCompanyId);

        // Invite and bid only for item 1 (Laptop), not item 2 (Monitor)
        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = 150000m,
            grandTotal = 165000m,
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId = itemId1,
                    brand = "Dell",
                    quantity = 10,
                    unitPrice = 15000m,
                    totalPrice = 150000m,
                    specs = new[]
                    {
                        new { key = "RAM", valueText = "16GB", valueNumber = (decimal?)16, unit = "GB", valueBoolean = (bool?)null }
                    }
                }
            }
        });

        var response = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}/comparison");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());

        // Monitor item — vendor bid should have null brand (no bid item for this RFP item)
        var monitorBids = items[1].GetProperty("bids");
        Assert.Equal(1, monitorBids.GetArrayLength());
        Assert.Equal(JsonValueKind.Null, monitorBids[0].GetProperty("brand").ValueKind);
        Assert.Equal(JsonValueKind.Null, monitorBids[0].GetProperty("unitPrice").ValueKind);
    }

    [Fact(DisplayName = "BID-05 comparison for RFP with no bids returns empty vendors")]
    public async Task BID05_NoBidsReturnsEmptyVendors()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var (rfpId, _, _) = await SeedRfpWith2ItemsAsync(buyer, buyerCompanyId);

        var response = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}/comparison");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("vendors").GetArrayLength());
        // Items still present even with no bids
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
    }

    [Fact(DisplayName = "BID-05 comparison for non-existent RFP returns 404")]
    public async Task BID05_NonExistentRfpReturns404()
    {
        var buyer = CreateBuyerClient();
        var response = await buyer.GetAsync("/api/buyer/rfps/99999/comparison");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "BID-05 comparison specs match between RFP requirement and bid offer")]
    public async Task BID05_SpecsMatchBetweenRequirementAndOffer()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, itemId1, itemId2) = await SeedRfpWith2ItemsAsync(buyer, buyerCompanyId);

        await InviteAndBidAsync(buyer, vendor, rfpId, vendorCompanyId, itemId1, itemId2,
            "Dell", 15000m, "LG", 5000m);

        var response = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}/comparison");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var laptopItem = body.GetProperty("items")[0];

        // Required specs: RAM + SSD
        var reqSpecs = laptopItem.GetProperty("requiredSpecs");
        var reqKeys = Enumerable.Range(0, reqSpecs.GetArrayLength())
            .Select(i => reqSpecs[i].GetProperty("key").GetString()).OrderBy(k => k).ToList();
        Assert.Equal(new[] { "RAM", "SSD" }, reqKeys);

        // Vendor bid specs for Laptop
        var bidSpecs = laptopItem.GetProperty("bids")[0].GetProperty("specs");
        var bidKeys = Enumerable.Range(0, bidSpecs.GetArrayLength())
            .Select(i => bidSpecs[i].GetProperty("key").GetString()).OrderBy(k => k).ToList();
        Assert.Equal(new[] { "RAM", "SSD" }, bidKeys);
    }
}
