namespace VisitorManagementSystem.Api.Application.DTOs.Companies
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? TaxId { get; set; }
        public string? ContactPersonName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Street1 { get; set; }
        public string? Street2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public int? EmployeeCount { get; set; }
        public string? Description { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public int VisitorCount { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }

    public class CreateCompanyDto
    {
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? TaxId { get; set; }
        public string? ContactPersonName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Street1 { get; set; }
        public string? Street2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public int? EmployeeCount { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateCompanyDto
    {
        public required string Name { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? ContactPersonName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Street1 { get; set; }
        public string? Street2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public int? EmployeeCount { get; set; }
        public string? Description { get; set; }
    }
}
