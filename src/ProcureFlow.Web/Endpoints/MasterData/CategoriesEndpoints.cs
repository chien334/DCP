using ProcureFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Web.Endpoints.MasterData;

public static class CategoriesEndpoints
{
    public static RouteGroupBuilder MapCategoriesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/categories", GetCategoriesAsync);
        group.MapGet("/categories/tree", GetCategoryTreeAsync);
        return group;
    }

    private static async Task<IResult> GetCategoriesAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = await dbContext.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.CategoryName)
            .Select(x => new CategoryFlatItem(
                x.Id,
                x.CategoryCode,
                x.CategoryName,
                x.ParentId,
                x.TreePath,
                x.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Results.Ok(new CategoryFlatResponse(items));
    }

    private static async Task<IResult> GetCategoryTreeAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.CategoryName)
            .Select(x => new CategoryNode(x.Id, x.CategoryCode, x.CategoryName, x.ParentId, x.TreePath, new List<CategoryNode>()))
            .ToListAsync(cancellationToken);

        var byId = categories.ToDictionary(x => x.Id);
        var roots = new List<CategoryNode>();

        foreach (var node in categories)
        {
            if (node.ParentId is null)
            {
                roots.Add(node);
                continue;
            }

            if (byId.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
        }

        return Results.Ok(new CategoryTreeResponse(roots));
    }
}

public sealed record CategoryFlatItem(
    int Id,
    string CategoryCode,
    string CategoryName,
    int? ParentId,
    string TreePath,
    int DisplayOrder);

public sealed record CategoryFlatResponse(IReadOnlyCollection<CategoryFlatItem> Items);

public sealed record CategoryNode(
    int Id,
    string CategoryCode,
    string CategoryName,
    int? ParentId,
    string TreePath,
    List<CategoryNode> Children);

public sealed record CategoryTreeResponse(IReadOnlyCollection<CategoryNode> Roots);
