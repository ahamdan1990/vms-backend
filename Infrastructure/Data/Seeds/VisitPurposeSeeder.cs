using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds
{
    /// <summary>
    /// Seeder for Visit Purposes
    /// </summary>
    public static class VisitPurposeSeeder
    {
        /// <summary>
        /// Gets seed visit purposes for initial database setup
        /// </summary>
        public static List<VisitPurpose> GetSeedVisitPurposes()
        {
            var purposes = new List<VisitPurpose>
            {
                new VisitPurpose
                {
                    Name = "Business Meeting",
                    Code = "BUSINESS_MEETING",
                    ColorCode = "#0078d4",
                    IconName = "Briefcase",
                    DisplayOrder = 1,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 4,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Interview / Recruitment",
                    Code = "INTERVIEW",
                    ColorCode = "#28a745",
                    IconName = "UserCheck",
                    DisplayOrder = 2,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 3,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Delivery / Courier",
                    Code = "DELIVERY",
                    ColorCode = "#ffc107",
                    IconName = "Truck",
                    DisplayOrder = 3,
                    RequiresApproval = false,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 2,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Maintenance / Technical Support",
                    Code = "MAINTENANCE",
                    ColorCode = "#17a2b8",
                    IconName = "Tools",
                    DisplayOrder = 4,
                    RequiresApproval = true,
                    RequiresSecurityClearance = true,
                    MaxDurationHours = 6,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Training / Workshop",
                    Code = "TRAINING",
                    ColorCode = "#6f42c1",
                    IconName = "BookOpen",
                    DisplayOrder = 5,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 8,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Vendor / Supplier Visit",
                    Code = "VENDOR",
                    ColorCode = "#e83e8c",
                    IconName = "Handshake",
                    DisplayOrder = 6,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 4,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Customer Visit",
                    Code = "CUSTOMER",
                    ColorCode = "#fd7e14",
                    IconName = "UserGroup",
                    DisplayOrder = 7,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 4,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Guest / Personal Visit",
                    Code = "GUEST",
                    ColorCode = "#6c757d",
                    IconName = "User",
                    DisplayOrder = 8,
                    RequiresApproval = false,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 2,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Audit / Inspection",
                    Code = "AUDIT",
                    ColorCode = "#20c997",
                    IconName = "ClipboardCheck",
                    DisplayOrder = 9,
                    RequiresApproval = true,
                    RequiresSecurityClearance = true,
                    MaxDurationHours = 5,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Facility Tour",
                    Code = "FACILITY_TOUR",
                    ColorCode = "#fd7e14",
                    IconName = "Map",
                    DisplayOrder = 10,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 3,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Medical / Health Check",
                    Code = "MEDICAL",
                    ColorCode = "#dc3545",
                    IconName = "Heart",
                    DisplayOrder = 11,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 2,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Official Government Visit",
                    Code = "GOVERNMENT",
                    ColorCode = "#0078d4",
                    IconName = "Government",
                    DisplayOrder = 12,
                    RequiresApproval = true,
                    RequiresSecurityClearance = true,
                    MaxDurationHours = 6,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Event / Conference / Seminar",
                    Code = "EVENT",
                    ColorCode = "#6610f2",
                    IconName = "Calendar",
                    DisplayOrder = 13,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 8,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Contractor / Construction Work",
                    Code = "CONTRACTOR",
                    ColorCode = "#6f42c1",
                    IconName = "HardHat",
                    DisplayOrder = 14,
                    RequiresApproval = true,
                    RequiresSecurityClearance = true,
                    MaxDurationHours = 8,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Emergency Response",
                    Code = "EMERGENCY",
                    ColorCode = "#dc3545",
                    IconName = "AlertTriangle",
                    DisplayOrder = 15,
                    RequiresApproval = false,
                    RequiresSecurityClearance = true,
                    MaxDurationHours = 0,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Sales / Demo Presentation",
                    Code = "SALES",
                    ColorCode = "#20c997",
                    IconName = "Presentation",
                    DisplayOrder = 16,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 4,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Legal / Compliance",
                    Code = "LEGAL",
                    ColorCode = "#343a40",
                    IconName = "Gavel",
                    DisplayOrder = 17,
                    RequiresApproval = true,
                    RequiresSecurityClearance = true,
                    MaxDurationHours = 4,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Investor / Partner Visit",
                    Code = "INVESTOR",
                    ColorCode = "#0078d4",
                    IconName = "UserTie",
                    DisplayOrder = 18,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 4,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Media / Press",
                    Code = "MEDIA",
                    ColorCode = "#fd7e14",
                    IconName = "Camera",
                    DisplayOrder = 19,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 3,
                    IsDefault = true
                },
                new VisitPurpose
                {
                    Name = "Student / Internship Visit",
                    Code = "STUDENT",
                    ColorCode = "#6f42c1",
                    IconName = "UserGraduate",
                    DisplayOrder = 20,
                    RequiresApproval = true,
                    RequiresSecurityClearance = false,
                    MaxDurationHours = 4,
                    IsDefault = true
                }
            };

            return purposes;
        }
    }
}
