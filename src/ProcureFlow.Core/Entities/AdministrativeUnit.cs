namespace ProcureFlow.Core.Entities;

public class AdministrativeUnit
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentCode { get; set; }
    public int Level { get; set; }
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
}
