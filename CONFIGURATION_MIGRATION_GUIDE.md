# 🔄 Configuration Migration Guide

## 📋 **How Default Values are Seeded**

The system now uses a **smart migration approach** that:

1. **Reads your existing appsettings.json** values
2. **Migrates them to the database** on first run
3. **Adds missing defaults** for any configurations not found
4. **Preserves your current settings** while enabling dynamic management

## 🚀 **Migration Process**

### **What Happens Automatically:**
1. On first app start, the system checks if configurations exist in database
2. If not, it reads from your current `appsettings.json`
3. Migrates JWT, Security, and Lockout settings to database
4. Adds any missing configurations with sensible defaults
5. Logs the migration process

### **Your Current Settings Are Preserved!**
The migration seeder will read values like:
```json
{
  "JWT": {
    "SecretKey": "your-actual-secret",
    "Issuer": "YourCompany",
    "Audience": "YourApp",
    "ExpiryInMinutes": 30
  },
  "Security": {
    "RequireHttps": true,
    "SessionTimeoutMinutes": 45
  },
  "Lockout": {
    "MaxFailedAttempts": 3,
    "LockoutDuration": "00:30:00"
  }
}
```

## 📝 **What to Keep vs Remove from appsettings.json**

### ✅ **KEEP in appsettings.json** (Infrastructure/Static):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-database-connection"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5000" }
    }
  },
  "Environment": "Production",
  "Database": {
    "CommandTimeout": 30,
    "EnableSensitiveDataLogging": false
  }
}
```

### ❌ **REMOVE from appsettings.json** (After Migration):
```json
{
  // These will be moved to database
  "JWT": { ... },           // → Database
  "Security": { ... },      // → Database  
  "Lockout": { ... }        // → Database
}
```

## 🔧 **Step-by-Step Migration Instructions**

### **Step 1: Run Initial Migration**
```bash
# Your current appsettings.json will be read and migrated
dotnet run
```

### **Step 2: Verify Migration Success**
Check the logs for:
```
Successfully migrated X configurations to database
```

### **Step 3: Test Configuration Access**
```bash
# Verify configurations are accessible
GET /api/admin/configuration
```

### **Step 4: Gradually Clean Up appsettings.json**

**Create a backup first:**
```bash
cp appsettings.json appsettings.json.backup
```

**Then remove migrated sections:**
```json
{
  // KEEP THESE
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  // REMOVE THESE (now in database)
  // "JWT": { ... },        <- REMOVE
  // "Security": { ... },   <- REMOVE  
  // "Lockout": { ... }     <- REMOVE
}
```

### **Step 5: Test After Cleanup**
```bash
# Restart app to ensure it works without JWT config in appsettings.json
dotnet run
```

## 🧪 **Safe Migration Strategy**

### **Phase 1: Dual Mode (Recommended)**
Keep both for a few days to ensure stability:
- Database configurations are primary
- appsettings.json as fallback
- Monitor logs for any issues

### **Phase 2: Database Only**
After confirming everything works:
- Remove dynamic settings from appsettings.json
- Keep only infrastructure settings

## 📊 **Verification Commands**

### **Check What Was Migrated:**
```bash
# View all configurations
GET /api/admin/configuration

# Check specific category
GET /api/admin/configuration/JWT

# View migration audit
GET /api/admin/configuration/JWT/SecretKey/history
```

### **Test Dynamic Updates:**
```bash
# Update a setting
PUT /api/admin/configuration/JWT/ExpiryInMinutes
{
  "value": "20",
  "reason": "Testing dynamic updates"
}

# Verify it took effect immediately (no restart needed)
```

## 🚨 **Important Notes**

### **Connection Strings**
**NEVER** move database connection strings to the database (circular dependency!)

### **Sensitive Settings**
JWT SecretKey will be marked as `IsSensitive = true` and masked in API responses.

### **Environment-Specific Settings**
Settings can be configured per environment:
- `Environment = "All"` (default)
- `Environment = "Development"`
- `Environment = "Production"`

### **Rollback Plan**
If needed, you can rollback by:
1. Restoring your appsettings.json backup
2. Clearing the SystemConfigurations table
3. Restarting the application

## 🎯 **Example Migration Result**

**Before (appsettings.json):**
```json
{
  "JWT": {
    "SecretKey": "my-secret-key",
    "ExpiryInMinutes": 30
  }
}
```

**After (Database):**
| Category | Key | Value | Description |
|----------|-----|-------|-------------|
| JWT | SecretKey | my-secret-key | Secret key for JWT signing |
| JWT | ExpiryInMinutes | 30 | Token expiry time |

**Updated appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Logging": { ... }
  // JWT section removed - now in database!
}
```

## ✅ **Benefits After Migration**

1. **No Deployments** for configuration changes
2. **Audit Trail** for all setting modifications
3. **Real-time Updates** without app restarts
4. **Role-based Management** through admin API
5. **Environment-specific** configurations
6. **Type-safe Access** in your code

## 🔧 **Troubleshooting**

### **Migration Failed?**
Check logs for specific errors and ensure:
- Database connection is working
- User permissions are correct
- appsettings.json syntax is valid

### **Configuration Not Loading?**
Verify:
- Migration completed successfully
- Cache is not stale (wait 30 minutes or invalidate)
- User has proper permissions

### **Need to Re-migrate?**
```sql
-- Clear configurations and re-run
DELETE FROM ConfigurationAudits;
DELETE FROM SystemConfigurations;
-- Then restart the application
```

Your settings are now ready for dynamic management! 🚀
