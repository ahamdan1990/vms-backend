using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeder for initial user data
/// </summary>
public static class UserSeeder
{
    /// <summary>
    /// Gets seed users for initial database setup
    /// </summary>
    /// <returns>List of seed users</returns>
    public static List<User> GetSeedUsers()
    {
        var users = new List<User>();

        // System Administrator
        var adminUser = CreateUser(
            firstName: "System",
            lastName: "Administrator",
            email: "admin@vms.com",
            role: UserRole.Administrator,
            department: "IT",
            jobTitle: "System Administrator",
            employeeId: "SYS001"
        );
        users.Add(adminUser);

        // Default Staff User
        var staffUser = CreateUser(
            firstName: "John",
            lastName: "Staff",
            email: "staff@vms.com",
            role: UserRole.Staff,
            department: "Human Resources",
            jobTitle: "HR Manager",
            employeeId: "HR001"
        );
        users.Add(staffUser);

        // Default Operator User
        var operatorUser = CreateUser(
            firstName: "Jane",
            lastName: "Receptionist",
            email: "operator@vms.com",
            role: UserRole.Receptionist,
            department: "Security",
            jobTitle: "Security Officer",
            employeeId: "SEC001"
        );
        users.Add(operatorUser);

        return users;
    }

    /// <summary>
    /// Creates a user with hashed password
    /// </summary>
    /// <param name="firstName">First name</param>
    /// <param name="lastName">Last name</param>
    /// <param name="email">Email address</param>
    /// <param name="role">User role</param>
    /// <param name="department">Department</param>
    /// <param name="jobTitle">Job title</param>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="password">Password (defaults to "Password123!")</param>
    /// <returns>User entity</returns>
    private static User CreateUser(
        string firstName,
        string lastName,
        string email,
        UserRole role,
        string? department = null,
        string? jobTitle = null,
        string? employeeId = null,
        string password = "Password123!")
    {
        var (passwordHash, passwordSalt) = HashPassword(password);

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = new Email(email),
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = role,
            Status = UserStatus.Active,
            Department = department,
            JobTitle = jobTitle,
            EmployeeId = employeeId,
            SecurityStamp = Guid.NewGuid().ToString(),
            TimeZone = "UTC",
            Language = "en-US",
            Theme = "light",
            PasswordChangedDate = DateTime.UtcNow,
            MustChangePassword = false,
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };

        return user;
    }

    /// <summary>
    /// Hashes a password using PBKDF2
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Tuple of password hash and salt</returns>
    private static (string hash, string salt) HashPassword(string password)
    {
        // Generate a random salt
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        var salt = Convert.ToBase64String(saltBytes);

        // Hash the password with the salt
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(32);
        var hash = Convert.ToBase64String(hashBytes);

        return (hash, salt);
    }

    /// <summary>
    /// Gets test users for development/testing
    /// </summary>
    /// <returns>List of test users</returns>
    public static List<User> GetTestUsers()
    {
        var users = new List<User>();

        // Additional test users for development
        for (int i = 1; i <= 10; i++)
        {
            var testUser = CreateUser(
                firstName: $"Test",
                lastName: $"User{i:D2}",
                email: $"test{i:D2}@vms.com",
                role: (UserRole)(i % 3 + 1), // Rotate through roles
                department: $"Department {(i % 5) + 1}",
                jobTitle: $"Position {i}",
                employeeId: $"TEST{i:D3}"
            );
            users.Add(testUser);
        }

        return users;
    }

    /// <summary>
    /// Creates a user with specific phone number
    /// </summary>
    /// <param name="firstName">First name</param>
    /// <param name="lastName">Last name</param>
    /// <param name="email">Email</param>
    /// <param name="phoneNumber">Phone number</param>
    /// <param name="role">User role</param>
    /// <returns>User with phone number</returns>
    public static User CreateUserWithPhone(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        UserRole role)
    {
        var user = CreateUser(firstName, lastName, email, role);
        user.PhoneNumber = (PhoneNumber?)phoneNumber;
        return user;
    }

    /// <summary>
    /// Gets users for load testing
    /// </summary>
    /// <param name="count">Number of users to create</param>
    /// <returns>List of load test users</returns>
    public static List<User> GetLoadTestUsers(int count)
    {
        var users = new List<User>();

        for (int i = 1; i <= count; i++)
        {
            var user = CreateUser(
                firstName: $"Load",
                lastName: $"Test{i:D6}",
                email: $"loadtest{i:D6}@vms.com",
                role: (UserRole)((i % 3) + 1),
                department: $"Load Test Dept {(i % 10) + 1}",
                employeeId: $"LOAD{i:D6}"
            );
            users.Add(user);
        }

        return users;
    }

    /// <summary>
    /// Validates that all seed users have unique emails and employee IDs
    /// </summary>
    /// <param name="users">List of users to validate</param>
    /// <returns>True if all users are valid</returns>
    public static bool ValidateSeedUsers(List<User> users)
    {
        // Check for duplicate emails
        var emails = users.Select(u => u.Email.Value).ToList();
        if (emails.Count != emails.Distinct().Count())
        {
            return false;
        }

        // Check for duplicate employee IDs
        var employeeIds = users.Where(u => !string.IsNullOrEmpty(u.EmployeeId))
                              .Select(u => u.EmployeeId!).ToList();
        if (employeeIds.Count != employeeIds.Distinct().Count())
        {
            return false;
        }

        // Validate each user
        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.FirstName) ||
                string.IsNullOrWhiteSpace(user.LastName) ||
                string.IsNullOrWhiteSpace(user.Email.Value) ||
                string.IsNullOrWhiteSpace(user.PasswordHash) ||
                string.IsNullOrWhiteSpace(user.PasswordSalt))
            {
                return false;
            }
        }

        return true;
    }
}