namespace ProcureFlow.Core.Entities;

public enum EmployeeStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

public class CompanyEmployee : IAuditable
{
    private CompanyEmployee()
    {
    }

    public CompanyEmployee(int companyId, string employeeCode, string fullName, string email)
    {
        CompanyId = companyId;
        EmployeeCode = employeeCode;
        FullName = fullName;
        Email = email;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public int Id { get; set; }
    public int CompanyId { get; private set; }
    public string EmployeeCode { get; private set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public Company Company { get; set; } = default!;
}