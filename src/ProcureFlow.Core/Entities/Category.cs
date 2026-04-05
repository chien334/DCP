namespace ProcureFlow.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentId { get; set; }
    public string TreePath { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
