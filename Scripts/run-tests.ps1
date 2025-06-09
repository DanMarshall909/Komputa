#!/usr/bin/env pwsh

# Komputa Test Execution Script
# Runs all tests including unit tests, BDD tests, and generates coverage reports

param(
    [switch]$Unit = $false,
    [switch]$BDD = $false,
    [switch]$Integration = $false,
    [switch]$All = $false,
    [switch]$Coverage = $false,
    [switch]$Mutation = $false,
    [switch]$Install = $false
)

Write-Host "🧠 Komputa Test Suite" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan

# Install required tools
if ($Install) {
    Write-Host "📦 Installing test tools..." -ForegroundColor Yellow
    
    # Install SpecFlow+ LivingDoc CLI
    dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI
    
    # Install ReportGenerator for coverage reports
    dotnet tool install --global dotnet-reportgenerator-globaltool
    
    # Install Stryker.NET for mutation testing
    dotnet tool install --global dotnet-stryker
    
    # Install coverlet for coverage collection
    dotnet add Tests/Komputa.Tests.Domain/Komputa.Tests.Domain.csproj package coverlet.msbuild
    dotnet add Tests/Komputa.Tests.Application/Komputa.Tests.Application.csproj package coverlet.msbuild
    dotnet add Tests/Komputa.Tests.Integration/Komputa.Tests.Integration.csproj package coverlet.msbuild
    
    Write-Host "✅ Test tools installed successfully" -ForegroundColor Green
    return
}

# Function to run unit tests
function Run-UnitTests {
    Write-Host "🔬 Running Unit Tests..." -ForegroundColor Yellow
    
    $testResult = dotnet test Tests/Komputa.Tests.Domain/Komputa.Tests.Domain.csproj `
        --logger "console;verbosity=detailed" `
        --collect:"XPlat Code Coverage" `
        --results-directory TestResults/Unit
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Unit tests passed" -ForegroundColor Green
    } else {
        Write-Host "❌ Unit tests failed" -ForegroundColor Red
        exit 1
    }
}

# Function to run BDD tests
function Run-BDDTests {
    Write-Host "🥒 Running BDD Tests..." -ForegroundColor Yellow
    
    # Run Application BDD tests
    $testResult = dotnet test Tests/Komputa.Tests.Application/Komputa.Tests.Application.csproj `
        --logger "console;verbosity=detailed" `
        --collect:"XPlat Code Coverage" `
        --results-directory TestResults/BDD/Application
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Application BDD tests failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ BDD tests passed" -ForegroundColor Green
}

# Function to run integration tests
function Run-IntegrationTests {
    Write-Host "🔗 Running Integration Tests..." -ForegroundColor Yellow
    
    $testResult = dotnet test Tests/Komputa.Tests.Integration/Komputa.Tests.Integration.csproj `
        --logger "console;verbosity=detailed" `
        --collect:"XPlat Code Coverage" `
        --results-directory TestResults/Integration
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Integration tests passed" -ForegroundColor Green
    } else {
        Write-Host "❌ Integration tests failed" -ForegroundColor Red
        exit 1
    }
}

# Function to generate coverage report
function Generate-CoverageReport {
    Write-Host "📊 Generating Coverage Report..." -ForegroundColor Yellow
    
    # Ensure TestResults directory exists
    if (!(Test-Path "TestResults")) {
        New-Item -ItemType Directory -Path "TestResults"
    }
    
    # Find all coverage files
    $coverageFiles = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse
    
    if ($coverageFiles.Count -eq 0) {
        Write-Host "⚠️  No coverage files found. Run tests with coverage collection first." -ForegroundColor Yellow
        return
    }
    
    # Generate combined coverage report
    $coverageArgs = $coverageFiles | ForEach-Object { "-reports:$($_.FullName)" }
    $reportArgs = @(
        $coverageArgs
        "-targetdir:TestResults/CoverageReport"
        "-reporttypes:Html;JsonSummary;Badges"
        "-title:Komputa Test Coverage"
    )
    
    & reportgenerator $reportArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Coverage report generated: TestResults/CoverageReport/index.html" -ForegroundColor Green
    } else {
        Write-Host "❌ Coverage report generation failed" -ForegroundColor Red
    }
}

# Function to run mutation testing
function Run-MutationTesting {
    Write-Host "🧬 Running Mutation Testing..." -ForegroundColor Yellow
    
    # Ensure the solution builds first
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed. Cannot run mutation testing." -ForegroundColor Red
        return
    }
    
    # Run Stryker mutation testing
    dotnet stryker --config-file stryker-config.json --output StrykerOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Mutation testing completed. Report: StrykerOutput/reports/mutation-report.html" -ForegroundColor Green
    } else {
        Write-Host "❌ Mutation testing failed" -ForegroundColor Red
    }
}

# Function to generate living documentation
function Generate-LivingDocs {
    Write-Host "📚 Generating Living Documentation..." -ForegroundColor Yellow
    
    # Generate SpecFlow living documentation
    livingdoc test-assembly Tests/Komputa.Tests.Application/bin/Debug/net8.0/Komputa.Tests.Application.dll `
        --output LivingDoc `
        --title "Komputa BDD Specifications"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Living documentation generated: LivingDoc/LivingDoc.html" -ForegroundColor Green
    } else {
        Write-Host "❌ Living documentation generation failed" -ForegroundColor Red
    }
}

# Main execution logic
try {
    # Build solution first
    Write-Host "🔨 Building solution..." -ForegroundColor Yellow
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build successful" -ForegroundColor Green
    
    # Run tests based on parameters
    if ($All -or (!$Unit -and !$BDD -and !$Integration -and !$Coverage -and !$Mutation)) {
        Write-Host "🎯 Running all tests..." -ForegroundColor Cyan
        Run-UnitTests
        Run-BDDTests
        Run-IntegrationTests
        Generate-CoverageReport
        Generate-LivingDocs
    } else {
        if ($Unit) { Run-UnitTests }
        if ($BDD) { Run-BDDTests }
        if ($Integration) { Run-IntegrationTests }
        if ($Coverage) { Generate-CoverageReport }
        if ($Mutation) { Run-MutationTesting }
    }
    
    Write-Host "🎉 Test execution completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "💥 Test execution failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
