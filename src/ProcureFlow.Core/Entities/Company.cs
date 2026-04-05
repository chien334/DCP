namespace ProcureFlow.Core.Entities;

public enum CompanyStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

public class Company : IAuditable
{
    public int Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public CompanyStatus Status { get; set; } = CompanyStatus.Draft;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public CompanyAddress? Address { get; set; }
    public ICollection<CompanyEmployee> Employees { get; set; } = new List<CompanyEmployee>();
}