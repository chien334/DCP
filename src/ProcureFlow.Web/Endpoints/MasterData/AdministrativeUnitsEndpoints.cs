using ProcureFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.MasterData;

public static class AdministrativeUnitsEndpoints
{
    public static RouteGroupBuilder MapAdministrativeUnitsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/admin-units", GetAdministrativeUnitsAsync);
        return group;
    }

    private static async Task<IResult> GetAdministrativeUnitsAsync(
        [FromQuery] int? level,
        [FromQuery] string? parentCode,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (level.HasValue && (level < 1 || level > 4))
        {
            return Results.BadRequest(new { code = "INVALID_LEVEL_FILTER" });
        }

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = dbContext.AdministrativeUnits.AsNoTracking().AsQueryable();
        if (level.HasValue)
        {
            query = query.Where(x => x.Level == level.Value);
        }

        if (!string.IsNullOrWhiteSpace(parentCode))
        {
            parentCode = parentCode.Trim();
            query = query.Where(x => x.ParentCode == parentCode);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Level)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdministrativeUnitItem(x.Id, x.Code, x.Name, x.ParentCode, x.Level))
            .ToListAsync(cancellationToken);

        return Results.Ok(new AdministrativeUnitsResponse(total, page, pageSize, items));
    }
}

public sealed record AdministrativeUnitItem(int Id, string Code, string Name, string? ParentCode, int Level);
public sealed record AdministrativeUnitsResponse(int Total, int Page, int PageSize, IReadOnlyCollection<AdministrativeUnitItem> Items);
