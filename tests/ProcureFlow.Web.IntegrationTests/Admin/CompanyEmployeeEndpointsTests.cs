using System.Net;
using System.Net.Http.Json;
using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProcureFlow.Web.IntegrationTests.Admin;

public class CompanyEmployeeEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public CompanyEmployeeEndpointsTests(WebApplicationFactory<Program> factory)
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

    [Fact(DisplayName = "COMP-01 admin can POST company and receives 201")]
    public async Task COMP01_AdminCanCreateCompany()
    {
        var client = CreateAdminClient();
        var response = await client.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = "Alpha Procurement",
            shortName = "Alpha",
            taxCode = "TAX-001",
            email = "hello@alpha.test",
            phone = "0900000000",
            status = 1,
            address = new
            {
                country = "VN",
                province = "HCM",
                district = "Q1",
                ward = "Ben Nghe",
                addressLine = "1 Main St",
                postalCode = "700000"
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CompanyPayload>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal(1, (int)body.Status);
    }

    [Fact(DisplayName = "COMP-02 admin can PATCH company with concurrency checks")]
    public async Task COMP02_AdminCanUpdateCompany()
    {
        var client = CreateAdminClient();
        var created = await (await client.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = "Bravo Procurement",
            shortName = "Bravo",
            taxCode = "TAX-002",
            email = "hello@bravo.test",
            phone = "0900000001",
            status = 1,
            address = new
            {
                country = "VN",
                province = "HN",
                district = "Ba Dinh",
                ward = "Lien Quan",
                addressLine = "2 Main St",
                postalCode = "100000"
            }
        })).Content.ReadFromJsonAsync<CompanyPayload>();

        var response = await client.PatchAsJsonAsync($"/api/admin/companies/{created!.Id}", new
        {
            legalName = "Bravo Holdings",
            shortName = "Bravo",
            email = "updated@bravo.test",
            phone = "0900000019",
            status = 2,
            address = new
            {
                country = "VN",
                province = "HN",
                district = "Ba Dinh",
                ward = "Lien Quan",
                addressLine = "2 Main St",
                postalCode = "100000"
            },
            updatedAtUtc = created.UpdatedAtUtc
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "EMP-01/EMP-02/EMP-03 employee CRUD by company scope")]
    public async Task EMP_Create_List_Detail_Update_ByCompanyScope()
    {
        var client = CreateAdminClient();
        var companyA = await CreateCompanyAsync(client, "TAX-A");
        var companyB = await CreateCompanyAsync(client, "TAX-B");

        var createEmployeeResponse = await client.PostAsJsonAsync($"/api/admin/companies/{companyA.Id}/employees", new
        {
            employeeCode = "EMP-001",
            fullName = "Nguyen Van A",
            email = "a@alpha.test",
            phone = "0911111111",
            status = 1
        });
        Assert.Equal(HttpStatusCode.Created, createEmployeeResponse.StatusCode);
        var employee = await createEmployeeResponse.Content.ReadFromJsonAsync<EmployeePayload>();
        Assert.NotNull(employee);

        var listA = await client.GetFromJsonAsync<EmployeeListPayload>($"/api/admin/companies/{companyA.Id}/employees?page=1&pageSize=20");
        var listB = await client.GetFromJsonAsync<EmployeeListPayload>($"/api/admin/companies/{companyB.Id}/employees?page=1&pageSize=20");
        Assert.Single(listA!.Items);
        Assert.Empty(listB!.Items);

        var detailResponse = await client.GetAsync($"/api/admin/employees/{employee!.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var updateResponse = await client.PatchAsJsonAsync($"/api/admin/employees/{employee.Id}", new
        {
            fullName = "Nguyen Van B",
            email = "b@alpha.test",
            phone = "0922222222",
            status = 2,
            updatedAtUtc = employee.UpdatedAtUtc
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Fact(DisplayName = "COMP/EMP negative cases: invalid status, missing company, duplicate keys")]
    public async Task COMP_EMP_NegativeCases_AreValidated()
    {
        var client = CreateAdminClient();
        var company = await CreateCompanyAsync(client, "TAX-C");

        var duplicateTaxCode = await client.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = "Gamma Procurement",
            shortName = "Gamma",
            taxCode = "TAX-C",
            email = "hello@gamma.test",
            phone = "0900000100",
            status = 1,
            address = new
            {
                country = "VN",
                province = "DN",
                district = "Hai Chau",
                ward = "Thach Thang",
                addressLine = "3 Main St",
                postalCode = "550000"
            }
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateTaxCode.StatusCode);

        var missingCompany = await client.PostAsJsonAsync("/api/admin/companies/999999/employees", new
        {
            employeeCode = "EMP-404",
            fullName = "Missing Company",
            email = "missing@test",
            phone = "0900000999",
            status = 1
        });
        Assert.Equal(HttpStatusCode.NotFound, missingCompany.StatusCode);

        await client.PostAsJsonAsync($"/api/admin/companies/{company.Id}/employees", new
        {
            employeeCode = "EMP-DUP",
            fullName = "Duplicate 1",
            email = "dup1@test",
            phone = "0900000666",
            status = 1
        });
        var duplicateEmployee = await client.PostAsJsonAsync($"/api/admin/companies/{company.Id}/employees", new
        {
            employeeCode = "EMP-DUP",
            fullName = "Duplicate 2",
            email = "dup2@test",
            phone = "0900000777",
            status = 1
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateEmployee.StatusCode);

        var invalidStatus = await client.PatchAsJsonAsync($"/api/admin/companies/{company.Id}", new
        {
            legalName = "Invalid",
            shortName = "Invalid",
            email = "invalid@test",
            phone = "0900000888",
            status = 99,
            address = new
            {
                country = "VN",
                province = "DN",
                district = "Hai Chau",
                ward = "Thach Thang",
                addressLine = "3 Main St",
                postalCode = "550000"
            },
            updatedAtUtc = company.UpdatedAtUtc
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Role", "Admin");
        client.DefaultRequestHeaders.Add("X-User-Id", "integration-admin");
        return client;
    }

    private async Task<CompanyPayload> CreateCompanyAsync(HttpClient client, string taxCode)
    {
        var response = await client.PostAsJsonAsync("/api/admin/companies", new
        {
            legalName = $"Company {taxCode}",
            shortName = taxCode,
            taxCode,
            email = $"{taxCode.ToLowerInvariant()}@example.test",
            phone = "0900000000",
            status = 1,
            address = new
            {
                country = "VN",
                province = "HCM",
                district = "Q1",
                ward = "Ben Nghe",
                addressLine = "Main",
                postalCode = "700000"
            }
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompanyPayload>())!;
    }

    private sealed record CompanyPayload(int Id, int Status, DateTime UpdatedAtUtc);
    private sealed record EmployeePayload(int Id, int CompanyId, string EmployeeCode, DateTime UpdatedAtUtc);
    private sealed record EmployeeListPayload(int Total, int Page, int PageSize, List<EmployeePayload> Items);
}