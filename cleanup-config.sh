#!/bin/bash
# Configuration System Cleanup Script

echo "🧹 Starting Configuration System Cleanup..."

# Phase 1: Remove redundant seeder files (SAFE)
echo "📁 Phase 1: Removing redundant configuration seeders..."

# Check if files exist before removing
if [ -f "Infrastructure/Data/Seeds/SystemConfigurationSeeder.cs" ]; then
    echo "❌ Removing SystemConfigurationSeeder.cs..."
    rm Infrastructure/Data/Seeds/SystemConfigurationSeeder.cs
fi

if [ -f "Infrastructure/Data/Seeds/ConfigurationSeeder.cs" ]; then
    echo "❌ Removing ConfigurationSeeder.cs..."
    rm Infrastructure/Data/Seeds/ConfigurationSeeder.cs
fi

if [ -f "Infrastructure/Data/Seeds/ConfigurationMigrationSeeder.cs" ]; then
    echo "❌ Removing ConfigurationMigrationSeeder.cs..."
    rm Infrastructure/Data/Seeds/ConfigurationMigrationSeeder.cs
fi

if [ -f "Infrastructure/Data/Seeds/ComprehensiveConfigurationMigrationSeeder.cs" ]; then
    echo "❌ Removing ComprehensiveConfigurationMigrationSeeder.cs..."
    rm Infrastructure/Data/Seeds/ComprehensiveConfigurationMigrationSeeder.cs
fi

echo "✅ Phase 1 Complete: Redundant seeders removed"
echo ""
echo "📋 Remaining configuration files:"
echo "✅ ComprehensiveConfigurationSeeder.cs (942 lines, ~140 configs)"
echo ""
echo "🎯 Next Steps:"
echo "1. Update ServiceCollectionExtensions.cs (remove static config registrations)"
echo "2. Update middleware to use IDynamicConfigurationService"
echo "3. Update authentication handlers"
echo "4. Test the application"
echo ""
echo "🚀 Your configuration system is now cleaned up!"
