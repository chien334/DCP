using System.Net;
using System.Net.Http.Json;
using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProcureFlow.Web.IntegrationTests.Buyer;

public class RfpEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public RfpEndpointsTests(WebApplicationFactory<Program> factory)
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

    private HttpClient CreateBuyerClient(string userId = "buyer-01")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Role", "Buyer");
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        return client;
    }

    private HttpClient CreateAdminClient(string userId = "admin-01")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Role", "Admin");
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        return client;
    }

    private async Task<int> SeedCompanyAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = "RFP Test Corp",
            shortName = "RTC",
            taxCode = $"TAX-{Guid.NewGuid():N}"[..16],
            email = "rfp@test.corp",
            phone = "0900000001",
            status = 1,
            address = new { country = "VN", province = "HCM", district = "Q1", ward = "BN", addressLine = "1 St", postalCode = "700000" }
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<IdPayload>();
        return body!.Id;
    }

    private int GetSeededCategoryId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return db.Categories.First().Id;
    }

    // ── RFP-01: Buyer can create RFP ────────────────────────────────────────────

    [Fact(DisplayName = "RFP-01 buyer can create RFP with nested items, specs and attachments")]
    public async Task RFP01_CreateRfpWithNestedPayload()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var companyId = await SeedCompanyAsync(admin);
        var categoryId = GetSeededCategoryId();

        var response = await buyer.PostAsJsonAsync("/api/buyer/rfps", new
        {
            companyId,
            title = "Office Supplies RFP",
            description = "Annual procurement of office supplies",
            budgetMin = 10000m,
            budgetMax = 50000m,
            categoryId,
            note = "Urgent requirement",
            type = 1,
            privacyMode = 1,
            deadline = DateTime.UtcNow.AddDays(30),
            items = new[]
            {
                new
                {
                    name = "RAM DDR5 32GB",
                    quantity = 100,
                    unit = "pieces",
                    note = "Kingston preferred",
                    specs = new[]
                    {
                        new { key = "Capacity", valueText = "32GB", valueNumber = (decimal?)32, valueBoolean = (bool?)null, unit = "GB" },
                        new { key = "DDR5", valueText = (string?)null, valueNumber = (decimal?)null, valueBoolean = (bool?)true, unit = (string?)null },
                    }
                },
                new
                {
                    name = "SSD NVMe 1TB",
                    quantity = 50,
                    unit = "pieces",
                    note = (string?)null,
                    specs = new[]
                    {
                        new { key = "Capacity", valueText = "1TB", valueNumber = (decimal?)1000, valueBoolean = (bool?)null, unit = "GB" },
                    }
                }
            },
            attachments = new[]
            {
                new { fileName = "specs.pdf", fileUrl = "https://storage.example.com/specs.pdf" },
                new { fileName = "budget.xlsx", fileUrl = "https://storage.example.com/budget.xlsx" },
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rfp = await db.Rfps
            .Include(r => r.Items).ThenInclude(i => i.Specs)
            .Include(r => r.Attachments)
            .FirstAsync(r => r.Title == "Office Supplies RFP");

        Assert.Equal(2, rfp.Items.Count);
        Assert.Equal(2, rfp.Items.First(i => i.Name == "RAM DDR5 32GB").Specs.Count);
        Assert.Single(rfp.Items.First(i => i.Name == "SSD NVMe 1TB").Specs);
        Assert.Equal(2, rfp.Attachments.Count);
        Assert.Equal(companyId, rfp.CompanyId);
        Assert.Equal(categoryId, rfp.CategoryId);
    }

    // ── RFP-01 negative: missing company ────────────────────────────────────────

    [Fact(DisplayName = "RFP-01 create with non-existent company returns 404")]
    public async Task RFP01_MissingCompany_Returns404()
    {
        var buyer = CreateBuyerClient();
        var categoryId = GetSeededCategoryId();

        var response = await buyer.PostAsJsonAsync("/api/buyer/rfps", new
        {
            companyId = 999999,
            title = "Ghost RFP",
            description = "Should fail",
            categoryId,
            type = 1,
            privacyMode = 1,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── RFP-01 negative: missing category ───────────────────────────────────────

    [Fact(DisplayName = "RFP-01 create with non-existent category returns 422")]
    public async Task RFP01_MissingCategory_Returns422()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var companyId = await SeedCompanyAsync(admin);

        var response = await buyer.PostAsJsonAsync("/api/buyer/rfps", new
        {
            companyId,
            title = "Invalid Cat RFP",
            description = "Should fail",
            categoryId = 999999,
            type = 1,
            privacyMode = 1,
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ── RFP-05: Buyer can list RFPs ──────────────────────────────────────────────

    [Fact(DisplayName = "RFP-05 buyer can list RFPs with pagination")]
    public async Task RFP05_ListRfps_ReturnsPaginated()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var companyId = await SeedCompanyAsync(admin);
        var categoryId = GetSeededCategoryId();

        // Create 2 RFPs
        for (int i = 1; i <= 2; i++)
        {
            await buyer.PostAsJsonAsync("/api/buyer/rfps", new
            {
                companyId,
                title = $"List Test RFP {i}",
                description = "For list test",
                categoryId,
                type = 1,
                privacyMode = 1,
            });
        }

        var response = await buyer.GetAsync($"/api/buyer/rfps?companyId={companyId}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RfpListPayload>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Total);
        Assert.Equal(2, body.Items.Count);
    }

    // ── RFP-05: Buyer can view RFP detail ────────────────────────────────────────

    [Fact(DisplayName = "RFP-05 buyer can filter RFP list by status")]
    public async Task RFP05_ListRfps_CanFilterByStatus()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var companyId = await SeedCompanyAsync(admin);
        var categoryId = GetSeededCategoryId();

        var draftResponse = await buyer.PostAsJsonAsync("/api/buyer/rfps", new
        {
            companyId,
            title = "Draft RFP",
            description = "Status filter draft",
            categoryId,
            type = 1,
            privacyMode = 1,
        });
        draftResponse.EnsureSuccessStatusCode();

        var closedResponse = await buyer.PostAsJsonAsync("/api/buyer/rfps", new
        {
            companyId,
            title = "Closed RFP",
            description = "Status filter closed",
            categoryId,
            type = 1,
            privacyMode = 1,
        });
        closedResponse.EnsureSuccessStatusCode();
        var closedId = (await closedResponse.Content.ReadFromJsonAsync<IdPayload>())!.Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var closedRfp = await db.Rfps.SingleAsync(r => r.Id == closedId);
            closedRfp.Status = RfpStatus.Closed;
            await db.SaveChangesAsync();
        }

        var response = await buyer.GetAsync($"/api/buyer/rfps?companyId={companyId}&status={(int)RfpStatus.Closed}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RfpListPayload>();
        Assert.NotNull(body);
        Assert.Single(body!.Items);
        Assert.Equal(closedId, body.Items[0].Id);
        Assert.Equal((int)RfpStatus.Closed, body.Items[0].Status);
    }

    [Fact(DisplayName = "RFP-05 buyer can view RFP detail with items, specs, attachments")]
    public async Task RFP05_GetRfpDetail_ReturnsFullAggregate()
    {
        var admin = CreateAdminClient();
        var buyer = CreateBuyerClient();
        var companyId = await SeedCompanyAsync(admin);
        var categoryId = GetSeededCategoryId();

        var createResponse = await buyer.PostAsJsonAsync("/api/buyer/rfps", new
        {
            companyId,
            title = "Detail Test RFP",
            description = "Full aggregate test",
            budgetMin = 5000m,
            budgetMax = 20000m,
            categoryId,
            type = 1,
            privacyMode = 1,
            items = new[]
            {
                new
                {
                    name = "Monitor 27 inch",
                    quantity = 20,
                    unit = "pieces",
                    note = (string?)null,
                    specs = new[]
                    {
                        new { key = "Resolution", valueText = "4K", valueNumber = (decimal?)null, valueBoolean = (bool?)null, unit = (string?)null }
                    }
                }
            },
            attachments = new[]
            {
                new { fileName = "rfp-doc.pdf", fileUrl = "https://storage.example.com/rfp-doc.pdf" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<IdPayload>();

        var detailResponse = await buyer.GetAsync($"/api/buyer/rfps/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var detail = await detailResponse.Content.ReadFromJsonAsync<RfpDetailPayload>();
        Assert.NotNull(detail);
        Assert.Equal("Detail Test RFP", detail!.Title);
        Assert.Single(detail.Items);
        Assert.Equal("Monitor 27 inch", detail.Items[0].Name);
        Assert.Single(detail.Items[0].Specs);
        Assert.Equal("Resolution", detail.Items[0].Specs[0].Key);
        Assert.Single(detail.Attachments);
        Assert.Equal("rfp-doc.pdf", detail.Attachments[0].FileName);
    }

    // ── RFP-05 negative: non-existent RFP ────────────────────────────────────────

    [Fact(DisplayName = "RFP-05 detail for non-existent RFP returns 404")]
    public async Task RFP05_NonExistentDetail_Returns404()
    {
        var buyer = CreateBuyerClient();
        var response = await buyer.GetAsync("/api/buyer/rfps/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

// ── Payload DTOs for deserialization ──────────────────────────────────────────

file sealed record IdPayload(int Id);

file sealed record RfpListPayload(int Total, int Page, int PageSize, List<RfpListItemPayload> Items);
file sealed record RfpListItemPayload(int Id, int CompanyId, string Title, decimal? BudgetMin, decimal? BudgetMax, int Status, DateTime? Deadline, DateTime CreatedAtUtc);

file sealed record RfpDetailPayload(
    int Id, int CompanyId, string Title, string Description,
    decimal? BudgetMin, decimal? BudgetMax, int CategoryId,
    string? Note, int Type, int PrivacyMode, int Status,
    DateTime? Deadline, DateTime CreatedAtUtc, string CreatedBy,
    DateTime UpdatedAtUtc, string UpdatedBy,
    List<RfpItemPayload> Items, List<RfpAttachmentPayload> Attachments);

file sealed record RfpItemPayload(int Id, string Name, int Quantity, string Unit, string? Note, int Status, List<RfpItemSpecPayload> Specs);
file sealed record RfpItemSpecPayload(int Id, string Key, string? ValueText, decimal? ValueNumber, bool? ValueBoolean, string? Unit, int Status);
file sealed record RfpAttachmentPayload(int Id, string FileName, string FileUrl, int Status);
