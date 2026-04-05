using System.Net;
using System.Net.Http.Json;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProcureFlow.Web.IntegrationTests.MasterData;

public class MasterDataReadEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public MasterDataReadEndpointsTests(WebApplicationFactory<Program> factory)
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

    [Fact(DisplayName = "MSTR-01 categories flat endpoint returns active categories with stable order")]
    public async Task MSTR01_CategoriesFlat_ReturnsStableOrder()
    {
        var client = CreateBuyerClient();
        var response = await client.GetAsync("/api/master-data/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CategoryFlatResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.Items.Count >= 3);

        var ordered = payload.Items.OrderBy(x => x.DisplayOrder).ThenBy(x => x.CategoryName).Select(x => x.Id).ToList();
        Assert.Equal(ordered, payload.Items.Select(x => x.Id).ToList());
    }

    [Fact(DisplayName = "MSTR-01 categories tree endpoint returns parent-child hierarchy")]
    public async Task MSTR01_CategoriesTree_ReturnsHierarchy()
    {
        var client = CreateBuyerClient();
        var response = await client.GetAsync("/api/master-data/categories/tree");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CategoryTreeResponse>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!.Roots);

        var itRoot = payload.Roots.Single(x => x.CategoryCode == "IT");
        Assert.Contains(itRoot.Children, x => x.CategoryCode == "IT-RAM");
    }

    [Fact(DisplayName = "MSTR-02 administrative units filter by level and parentCode")]
    public async Task MSTR02_AdminUnits_FilterByLevelAndParentCode()
    {
        var client = CreateBuyerClient();
        var response = await client.GetAsync("/api/master-data/admin-units?level=4&parentCode=VN-HCM-Q1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdministrativeUnitsResponse>();
        Assert.NotNull(payload);
        Assert.All(payload!.Items, x => Assert.Equal(4, x.Level));
        Assert.All(payload.Items, x => Assert.Equal("VN-HCM-Q1", x.ParentCode));
    }

    [Fact(DisplayName = "MasterDataReadEndpoints rejects invalid level filter")]
    public async Task MasterDataReadEndpoints_InvalidLevel_ReturnsBadRequest()
    {
        var client = CreateBuyerClient();
        var response = await client.GetAsync("/api/master-data/admin-units?level=99");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateBuyerClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Role", "Buyer");
        client.DefaultRequestHeaders.Add("X-User-Id", "buyer-integration");
        return client;
    }

    private sealed record CategoryFlatItem(int Id, string CategoryCode, string CategoryName, int? ParentId, string TreePath, int DisplayOrder);
    private sealed record CategoryFlatResponse(List<CategoryFlatItem> Items);
    private sealed record CategoryNode(int Id, string CategoryCode, string CategoryName, int? ParentId, string TreePath, List<CategoryNode> Children);
    private sealed record CategoryTreeResponse(List<CategoryNode> Roots);
    private sealed record AdministrativeUnitItem(int Id, string Code, string Name, string? ParentCode, int Level);
    private sealed record AdministrativeUnitsResponse(int Total, int Page, int PageSize, List<AdministrativeUnitItem> Items);
}
