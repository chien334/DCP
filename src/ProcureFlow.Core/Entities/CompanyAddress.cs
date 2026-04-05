namespace ProcureFlow.Core.Entities;

public class CompanyAddress
{
    public int CompanyId { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    public Company Company { get; set; } = default!;
}