using System.Net;
using System.Net.Http.Json;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProcureFlow.Web.IntegrationTests.Security;

public class RbacAndAuditTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public RbacAndAuditTests(WebApplicationFactory<Program> factory)
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

    private HttpClient CreateClient(string? role, string? userId = null)
    {
        var client = _factory.CreateClient();
        if (role is not null)
            client.DefaultRequestHeaders.Add("X-Role", role);
        if (userId is not null)
            client.DefaultRequestHeaders.Add("X-User-Id", userId);
        return client;
    }

    // ── AUD-02: RBAC — unauthenticated request returns 401 ──────────────────────

    [Fact(DisplayName = "AUD-02 unauthenticated request to admin endpoint returns 401")]
    public async Task AUD02_Unauthenticated_Returns401()
    {
        var client = CreateClient(role: null);
        var response = await client.GetAsync("/api/admin/companies/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── AUD-02: RBAC — Buyer cannot access admin write endpoint ─────────────────

    [Fact(DisplayName = "AUD-02 buyer cannot POST to admin endpoint — returns 403")]
    public async Task AUD02_BuyerCannotWriteAdmin_Returns403()
    {
        var client = CreateClient(role: "Buyer", userId: "buyer-01");
        var response = await client.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = "Buyer Attempt", shortName = "B", taxCode = "TAX-BUYER",
            email = "b@b.test", phone = "0900000099", status = 1,
            address = new { country = "VN", province = "HN", district = "X", ward = "Y", addressLine = "Z", postalCode = "000000" }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── AUD-02: RBAC — Vendor cannot access any protected endpoint ───────────────

    [Fact(DisplayName = "AUD-02 vendor is denied master-data GET — returns 403")]
    public async Task AUD02_VendorDeniedMasterData_Returns403()
    {
        var client = CreateClient(role: "Vendor", userId: "vendor-01");
        var response = await client.GetAsync("/api/master-data/categories");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── AUD-02: RBAC — Admin can access write endpoints ─────────────────────────

    [Fact(DisplayName = "AUD-02 admin role is allowed to POST admin endpoint — returns 201")]
    public async Task AUD02_AdminCanWrite_Returns201()
    {
        var client = CreateClient(role: "Admin", userId: "admin-01");
        var response = await client.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = "RBAC Test Co", shortName = "RTC", taxCode = "TAX-RBAC-01",
            email = "rbac@test", phone = "0900000001", status = 1,
            address = new { country = "VN", province = "HCM", district = "Q1", ward = "BN", addressLine = "1 St", postalCode = "700000" }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── AUD-01: Audit — create stamps actor and timestamp ────────────────────────

    [Fact(DisplayName = "AUD-01 create operation stamps createdBy/createdAt from actor")]
    public async Task AUD01_CreateStampsActorAndTimestamp()
    {
        var actorId = "audit-actor-01";
        var client = CreateClient(role: "Admin", userId: actorId);

        var before = DateTime.UtcNow.AddSeconds(-1);

        var response = await client.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = "Audit Stamp Co", shortName = "AS", taxCode = "TAX-AUD-01",
            email = "aud@test", phone = "0900000002", status = 1,
            address = new { country = "VN", province = "HCM", district = "Q1", ward = "BN", addressLine = "1 St", postalCode = "700000" }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Inspect the DB directly
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var company = await db.Companies.FirstAsync(c => c.TaxCode == "TAX-AUD-01");

        Assert.Equal(actorId, company.CreatedBy);
        Assert.True(company.CreatedAtUtc >= before);
        Assert.Equal(company.CreatedBy, company.UpdatedBy);
        Assert.Equal(company.CreatedAtUtc, company.UpdatedAtUtc);
    }

    // ── AUD-01: Audit — update stamps updatedBy without mutating createdBy ───────

    [Fact(DisplayName = "AUD-01 update stamps updatedBy without overwriting createdBy")]
    public async Task AUD01_UpdateStampsUpdatedBy_NotCreatedBy()
    {
        var creatorId = "creator-01";
        var updaterId = "updater-01";

        // Create as creator
        var creatorClient = CreateClient(role: "Admin", userId: creatorId);
        var createResponse = await creatorClient.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = "Update Audit Co", shortName = "UA", taxCode = "TAX-AUD-02",
            email = "ua@test", phone = "0900000003", status = 1,
            address = new { country = "VN", province = "HCM", district = "Q1", ward = "BN", addressLine = "1 St", postalCode = "700000" }
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyAuditPayload>();

        // Update as updater
        var updaterClient = CreateClient(role: "Admin", userId: updaterId);
        var patchResponse = await updaterClient.PatchAsJsonAsync($"/api/admin/companies/{created!.Id}", new
        {
            legalName = "Update Audit Co v2", shortName = "UA", email = "ua2@test", phone = "0900000003",
            status = 2,
            address = new { country = "VN", province = "HCM", district = "Q1", ward = "BN", addressLine = "1 St", postalCode = "700000" },
            updatedAtUtc = created.UpdatedAtUtc
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Verify DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var company = await db.Companies.FirstAsync(c => c.TaxCode == "TAX-AUD-02");

        Assert.Equal(creatorId, company.CreatedBy);  // unchanged
        Assert.Equal(updaterId, company.UpdatedBy);  // stamped by interceptor
        Assert.True(company.UpdatedAtUtc > company.CreatedAtUtc);
    }
}

// DTO projections for audit assertions
file sealed record CompanyAuditPayload(int Id, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, string CreatedBy, string UpdatedBy);
