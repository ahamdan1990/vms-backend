using Microsoft.AspNetCore.Authorization;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Authorization;

/// <summary>
/// Permission requirement implementation (already defined in PermissionHandler.cs)
/// This file exists to maintain clean separation if needed
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public override string ToString()
    {
        return $"Permission: {Permission}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is PermissionRequirement other)
        {
            return Permission.Equals(other.Permission, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Permission.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Resource-based permission requirement
/// </summary>
public class ResourcePermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public string Resource { get; }

    public ResourcePermissionRequirement(string permission, string resource)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }

    public override string ToString()
    {
        return $"Permission: {Permission}, Resource: {Resource}";
    }
}

/// <summary>
/// Owner-based permission requirement (user can only access their own resources)
/// </summary>
public class OwnerPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public string ResourceIdParameterName { get; }

    public OwnerPermissionRequirement(string permission, string resourceIdParameterName = "id")
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        ResourceIdParameterName = resourceIdParameterName;
    }

    public override string ToString()
    {
        return $"Owner Permission: {Permission}, Parameter: {ResourceIdParameterName}";
    }
}

/// <summary>
/// Time-based permission requirement (access only during certain hours)
/// </summary>
public class TimeBasedPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    public DayOfWeek[]? AllowedDays { get; }

    public TimeBasedPermissionRequirement(
        string permission,
        TimeSpan startTime,
        TimeSpan endTime,
        DayOfWeek[]? allowedDays = null)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        StartTime = startTime;
        EndTime = endTime;
        AllowedDays = allowedDays;
    }

    public bool IsCurrentTimeAllowed()
    {
        var now = DateTime.Now;
        var currentTime = now.TimeOfDay;
        var currentDay = now.DayOfWeek;

        // Check day restriction
        if (AllowedDays != null && !AllowedDays.Contains(currentDay))
        {
            return false;
        }

        // Check time restriction
        if (StartTime <= EndTime)
        {
            // Same day range (e.g., 9:00 AM to 5:00 PM)
            return currentTime >= StartTime && currentTime <= EndTime;
        }
        else
        {
            // Overnight range (e.g., 10:00 PM to 6:00 AM)
            return currentTime >= StartTime || currentTime <= EndTime;
        }
    }

    public override string ToString()
    {
        var daysStr = AllowedDays != null ? $", Days: {string.Join(", ", AllowedDays)}" : "";
        return $"Time Permission: {Permission}, {StartTime}-{EndTime}{daysStr}";
    }
}

/// <summary>
/// IP-based permission requirement (access only from certain IP addresses)
/// </summary>
public class IPBasedPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public IEnumerable<string> AllowedIPs { get; }
    public IEnumerable<string> AllowedIPRanges { get; }

    public IPBasedPermissionRequirement(
        string permission,
        IEnumerable<string> allowedIPs,
        IEnumerable<string>? allowedIPRanges = null)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        AllowedIPs = allowedIPs ?? throw new ArgumentNullException(nameof(allowedIPs));
        AllowedIPRanges = allowedIPRanges ?? Enumerable.Empty<string>();
    }

    public override string ToString()
    {
        return $"IP Permission: {Permission}, IPs: {string.Join(", ", AllowedIPs)}";
    }
}