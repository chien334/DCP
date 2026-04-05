using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProcureFlow.Web.IntegrationTests.Vendor;

public class VendorBidEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public VendorBidEndpointsTests(WebApplicationFactory<Program> factory)
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

    private async Task<int> SeedCompanyAsync(HttpClient admin, string suffix = "")
    {
        var response = await admin.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = $"Test Corp {suffix}".Trim(),
            shortName = $"TC{suffix}",
            taxCode = $"TAX{Guid.NewGuid():N}"[..20],
            email = $"test{suffix}@example.com",
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
        // Seed a category first
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cat = new ProcureFlow.Core.Entities.Category
        {
            CategoryCode = $"CAT-{Guid.NewGuid():N}"[..20],
            CategoryName = "Test Category",
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
            title = "Test RFP",
            description = "Test description",
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
                        new { key = "RAM", valueText = "16GB", valueNumber = (decimal?)null, valueBoolean = (bool?)null, unit = "GB" }
                    }
                }
            }
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rfpId = body.GetProperty("id").GetInt32();

        // Get the RFP item ID
        var detailResp = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}");
        var detail = await detailResp.Content.ReadFromJsonAsync<JsonElement>();
        var rfpItemId = detail.GetProperty("items")[0].GetProperty("id").GetInt32();

        return (rfpId, rfpItemId);
    }

    // ── VEND-01: Buyer can invite vendor ─────────────────────────────────────────

    [Fact(DisplayName = "VEND-01 buyer can invite vendor to RFP")]
    public async Task VEND01_BuyerCanInviteVendor()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, _) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        var response = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("id").GetInt32() > 0);
    }

    [Fact(DisplayName = "VEND-01 duplicate invite returns 409")]
    public async Task VEND01_DuplicateInviteReturns409()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, _) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });
        var response = await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── VEND-02: List invites ────────────────────────────────────────────────────

    [Fact(DisplayName = "VEND-02 buyer can list invites for RFP")]
    public async Task VEND02_BuyerCanListInvites()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, _) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        var response = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}/invites");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var invites = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, invites.GetArrayLength());
        Assert.Equal(vendorCompanyId, invites[0].GetProperty("companyId").GetInt32());
    }

    // ── BID-01: Vendor can submit bid ────────────────────────────────────────────

    [Fact(DisplayName = "BID-01 vendor can submit bid for invited RFP")]
    public async Task BID01_VendorCanSubmitBid()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        // Invite vendor
        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        // Submit bid
        var response = await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = 100000m,
            grandTotal = 110000m,
            currency = "VND",
            proposal = "We offer the best laptops",
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId,
                    brand = "Dell",
                    quantity = 10,
                    unitPrice = 10000m,
                    totalPrice = 100000m,
                    note = (string?)null,
                    specs = new[]
                    {
                        new { key = "RAM", valueText = "16GB", valueNumber = (decimal?)16, valueBoolean = (bool?)null, unit = "GB" }
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("id").GetInt32() > 0);
    }

    [Fact(DisplayName = "BID-01 uninvited vendor cannot submit bid")]
    public async Task BID01_UninvitedVendorCannotBid()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        // No invite — submit directly
        var response = await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = 100000m,
            grandTotal = 110000m,
            privacyMode = 1,
            items = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "BID-01 duplicate bid returns 409")]
    public async Task BID01_DuplicateBidReturns409()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        var bidPayload = new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = 100000m,
            grandTotal = 110000m,
            privacyMode = 1,
            items = Array.Empty<object>()
        };

        await vendor.PostAsJsonAsync("/api/vendor/bids", bidPayload);
        var response = await vendor.PostAsJsonAsync("/api/vendor/bids", bidPayload);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "BID-01 vendor can update bid via PUT")]
    public async Task BID01_VendorCanUpdateBid()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites", new { companyId = vendorCompanyId });

        var createResponse = await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = 100000m,
            grandTotal = 110000m,
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId,
                    brand = "Dell",
                    quantity = 10,
                    unitPrice = 10000m,
                    totalPrice = 100000m,
                    note = (string?)null,
                    specs = Array.Empty<object>()
                }
            }
        });

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bidId = createBody.GetProperty("id").GetInt32();

        var updateResponse = await vendor.PutAsJsonAsync($"/api/vendor/bids/{bidId}", new
        {
            companyId = vendorCompanyId,
            vatRate = 8.0m,
            subTotal = 120000m,
            grandTotal = 129600m,
            currency = "VND",
            proposal = "Updated proposal",
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId,
                    brand = "Lenovo",
                    quantity = 10,
                    unitPrice = 12000m,
                    totalPrice = 120000m,
                    note = "Updated item",
                    specs = Array.Empty<object>()
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var detailResponse = await vendor.GetAsync($"/api/vendor/bids/{bidId}?companyId={vendorCompanyId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(129600m, detail.GetProperty("grandTotal").GetDecimal());
        Assert.Equal("Updated proposal", detail.GetProperty("proposal").GetString());
        Assert.Equal("Lenovo", detail.GetProperty("items")[0].GetProperty("brand").GetString());
    }

    [Fact(DisplayName = "BID-01 update returns 422 for invalid RFP item")]
    public async Task BID01_UpdateReturns422ForInvalidRfpItem()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites", new { companyId = vendorCompanyId });

        var createResponse = await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = 100000m,
            grandTotal = 110000m,
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId,
                    brand = "Dell",
                    quantity = 10,
                    unitPrice = 10000m,
                    totalPrice = 100000m,
                    note = (string?)null,
                    specs = Array.Empty<object>()
                }
            }
        });

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bidId = createBody.GetProperty("id").GetInt32();

        var updateResponse = await vendor.PutAsJsonAsync($"/api/vendor/bids/{bidId}", new
        {
            companyId = vendorCompanyId,
            vatRate = 8.0m,
            subTotal = 120000m,
            grandTotal = 129600m,
            currency = "VND",
            proposal = "Updated proposal",
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId = 999999,
                    brand = "Lenovo",
                    quantity = 10,
                    unitPrice = 12000m,
                    totalPrice = 120000m,
                    note = "Invalid mapping",
                    specs = Array.Empty<object>()
                }
            }
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, updateResponse.StatusCode);
    }

    // ── BID-02/03: Bid detail with items and specs ──────────────────────────────

    [Fact(DisplayName = "BID-02 vendor can get bid detail with items and specs")]
    public async Task BID02_VendorCanGetBidDetail()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        var createResp = await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = 50000m,
            grandTotal = 55000m,
            currency = "VND",
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    rfpItemId,
                    brand = "HP",
                    quantity = 10,
                    unitPrice = 5000m,
                    totalPrice = 50000m,
                    note = (string?)null,
                    specs = new[]
                    {
                        new { key = "RAM", valueText = "8GB", valueNumber = (decimal?)8, valueBoolean = (bool?)null, unit = "GB" }
                    }
                }
            }
        });
        var createBody = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var bidId = createBody.GetProperty("id").GetInt32();

        var response = await vendor.GetAsync($"/api/vendor/bids/{bidId}?companyId={vendorCompanyId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(rfpId, detail.GetProperty("rfpId").GetInt32());
        Assert.Equal(vendorCompanyId, detail.GetProperty("companyId").GetInt32());
        Assert.Equal(55000m, detail.GetProperty("grandTotal").GetDecimal());

        var items = detail.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("HP", items[0].GetProperty("brand").GetString());
        Assert.Equal(rfpItemId, items[0].GetProperty("rfpItemId").GetInt32());

        var specs = items[0].GetProperty("specs");
        Assert.Equal(1, specs.GetArrayLength());
        Assert.Equal("RAM", specs[0].GetProperty("key").GetString());
    }

    // ── BID-04: Buyer can list bids by RFP ──────────────────────────────────────

    [Fact(DisplayName = "BID-04 buyer can list bids for an RFP")]
    public async Task BID04_BuyerCanListBidsByRfp()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites",
            new { companyId = vendorCompanyId });

        await vendor.PostAsJsonAsync("/api/vendor/bids", new
        {
            rfpId,
            companyId = vendorCompanyId,
            vatRate = 10.0m,
            subTotal = 100000m,
            grandTotal = 110000m,
            privacyMode = 1,
            items = Array.Empty<object>()
        });

        var response = await buyer.GetAsync($"/api/buyer/rfps/{rfpId}/bids");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bids = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, bids.GetArrayLength());
        Assert.Equal(vendorCompanyId, bids[0].GetProperty("companyId").GetInt32());
    }

    // ── RBAC: Vendor cannot access buyer endpoints ──────────────────────────────

    [Fact(DisplayName = "RBAC vendor gets 403 on buyer endpoint")]
    public async Task RBAC_VendorCannotAccessBuyerEndpoints()
    {
        var vendor = CreateVendorClient();
        var response = await vendor.GetAsync("/api/buyer/rfps");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "RBAC buyer gets 403 on vendor endpoint")]
    public async Task RBAC_BuyerCannotAccessVendorEndpoints()
    {
        var buyer = CreateBuyerClient();
        var response = await buyer.GetAsync("/api/vendor/bids");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "API-05 vendor can list invites for its company")]
    public async Task API05_VendorCanListInvitesForCompany()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, _) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites", new { companyId = vendorCompanyId });

        var response = await vendor.GetAsync($"/api/vendor/invites?companyId={vendorCompanyId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        Assert.Equal(rfpId, body.GetProperty("items")[0].GetProperty("rfpId").GetInt32());
    }

    [Fact(DisplayName = "API-05 vendor can load invited RFP detail")]
    public async Task API05_VendorCanLoadInvitedRfpDetail()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var vendor = CreateVendorClient();

        var buyerCompanyId = await SeedCompanyAsync(admin, "Buyer");
        var vendorCompanyId = await SeedCompanyAsync(admin, "Vendor");
        var (rfpId, rfpItemId) = await SeedRfpWithItemAsync(buyer, buyerCompanyId);

        await buyer.PostAsJsonAsync($"/api/buyer/rfps/{rfpId}/invites", new { companyId = vendorCompanyId });

        var response = await vendor.GetAsync($"/api/vendor/rfps/{rfpId}?companyId={vendorCompanyId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(rfpId, body.GetProperty("id").GetInt32());
        Assert.Equal(vendorCompanyId, body.GetProperty("vendorCompanyId").GetInt32());
        Assert.Equal(rfpItemId, body.GetProperty("items")[0].GetProperty("id").GetInt32());
    }
}
