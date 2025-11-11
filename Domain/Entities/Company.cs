using System;
using System.Collections.Generic;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Domain.Entities
{
    /// <summary>
    /// Company entity - organizations/companies that visitors come from
    /// Reusable across multiple visitors to avoid duplicate data entry
    /// </summary>
    public class Company : SoftDeleteEntity
    {
        /// <summary>
        /// Official company name (required)
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Short code or abbreviation (required)
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Website URL (optional)
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// Industry/Sector (optional)
        /// </summary>
        public string? Industry { get; set; }

        /// <summary>
        /// Tax ID or Registration Number (optional)
        /// </summary>
        public string? TaxId { get; set; }

        /// <summary>
        /// Contact person name (optional)
        /// </summary>
        public string? ContactPersonName { get; set; }

        /// <summary>
        /// Contact email (optional)
        /// </summary>
        public Email? Email { get; set; }

        /// <summary>
        /// Contact phone (optional)
        /// </summary>
        public PhoneNumber? PhoneNumber { get; set; }

        /// <summary>
        /// Company address (optional)
        /// </summary>
        public Address? Address { get; set; }

        /// <summary>
        /// Number of employees (optional)
        /// </summary>
        public int? EmployeeCount { get; set; }

        /// <summary>
        /// Company logo/image path (optional)
        /// </summary>
        public string? LogoPath { get; set; }

        /// <summary>
        /// Company description (optional)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Is company verified/approved
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Date company was verified
        /// </summary>
        public DateTime? VerifiedOn { get; set; }

        /// <summary>
        /// User who verified the company
        /// </summary>
        public int? VerifiedBy { get; set; }

        /// <summary>
        /// Reason if company is blacklisted (optional)
        /// </summary>
        public string? BlacklistReason { get; set; }

        /// <summary>
        /// Date company was blacklisted
        /// </summary>
        public DateTime? BlacklistedOn { get; set; }

        /// <summary>
        /// User who blacklisted the company
        /// </summary>
        public int? BlacklistedBy { get; set; }

        /// <summary>
        /// Display order
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Number of visitors from this company
        /// </summary>
        public int VisitorCount { get; set; } = 0;

        /// <summary>
        /// Visitors from this company
        /// </summary>
        public List<Visitor> Visitors { get; set; } = [];
    }
}
