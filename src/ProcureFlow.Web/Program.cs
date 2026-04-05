using System.Security.Claims;
using System.Text.Encodings.Web;
using ProcureFlow.Infrastructure.Data;
using ProcureFlow.Infrastructure.Data.Interceptors;
using ProcureFlow.Infrastructure.Data.Seed;
using ProcureFlow.Infrastructure.Audit;
using ProcureFlow.Web.Endpoints.Admin;
using ProcureFlow.Web.Endpoints.MasterData;
using ProcureFlow.Web.Endpoints.Buyer;
using ProcureFlow.Web.Endpoints.Vendor;
using ProcureFlow.Web.Security;
using ProcureFlow.Web.Middleware;
using ProcureFlow.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorContextAccessor, HttpActorContextAccessor>();
builder.Services.AddScoped<IAuditEventWriter, AuditEventWriter>();
builder.Services.AddScoped<AuditStampInterceptor>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var serverVersion = ServerVersion.AutoDetect(connectionString);

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
	options.UseMySql(connectionString, serverVersion);
	options.AddInterceptors(sp.GetRequiredService<AuditStampInterceptor>());
});
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();
builder.Services.AddAuthentication(HeaderAuthenticationHandler.SchemeName)
	.AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(HeaderAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
	{
		await dbContext.Database.MigrateAsync();
	}
	await MasterDataSeeder.SeedAsync(dbContext);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseRbacPolicy();
app.UseAuthorization();
app.UseAntiforgery();

var adminGroup = app.MapGroup("/api/admin")
	.RequireAuthorization(policy => policy.RequireRole("Admin"));
adminGroup.MapCompaniesEndpoints();
adminGroup.MapEmployeesEndpoints();

var masterDataGroup = app.MapGroup("/api/master-data")
	.RequireAuthorization(policy => policy.RequireRole("Admin", "Buyer"));
masterDataGroup.MapCategoriesEndpoints();
masterDataGroup.MapAdministrativeUnitsEndpoints();

var buyerGroup = app.MapGroup("/api/buyer")
	.RequireAuthorization(policy => policy.RequireRole("Admin", "Buyer"));
buyerGroup.MapRfpEndpoints();
buyerGroup.MapVendorInviteEndpoints();
buyerGroup.MapRfpBidReviewEndpoints();
buyerGroup.MapRfpFinalizeEndpoints();
buyerGroup.MapRfpContractEndpoints();

var vendorGroup = app.MapGroup("/api/vendor")
	.RequireAuthorization(policy => policy.RequireRole("Admin", "Vendor"));
vendorGroup.MapBidEndpoints();
vendorGroup.MapVendorContractEndpoints();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();

public partial class Program;

public class HeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	public const string SchemeName = "Header";

	public HeaderAuthenticationHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder)
		: base(options, logger, encoder)
	{
	}

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue("X-Role", out var role) || string.IsNullOrWhiteSpace(role))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var userId = Request.Headers.TryGetValue("X-User-Id", out var user) ? user.ToString() : "admin-user";
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, userId),
			new Claim(ClaimTypes.Name, userId),
			new Claim(ClaimTypes.Role, role.ToString())
		};

		var identity = new ClaimsIdentity(claims, SchemeName);
		var principal = new ClaimsPrincipal(identity);
		return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
	}
}
