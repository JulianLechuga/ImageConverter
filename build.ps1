<#
.SYNOPSIS
    Automated Build & Packaging Script for Local Image Converter
.DESCRIPTION
    1. Cleans previous artifacts
    2. Restores dependencies
    3. Builds in Release configuration
    4. Executes complete unit & integration test suite
    5. Publishes win-x64 self-contained standalone application
    6. Compiles Inno Setup installer executable (LocalImageConverter-Setup-1.0.0.exe)
#>

[CmdletBinding()]
param(
    [switch]$SkipTests = $false,
    [switch]$SkipInstaller = $false
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "     LOCAL IMAGE CONVERTER - AUTOMATED BUILD SCRIPT       " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. CLEAN
Write-Host "`n[1/6] Cleaning build and dist directories..." -ForegroundColor Yellow
$DistDir = Join-Path $ScriptDir "dist"
if (Test-Path $DistDir) {
    Remove-Item -Path $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir | Out-Null
dotnet clean -c Release -v minimal

# 2. RESTORE
Write-Host "`n[2/6] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# 3. BUILD
Write-Host "`n[3/6] Building solution in Release configuration..." -ForegroundColor Yellow
dotnet build -c Release --no-restore

# 4. TESTS
if (-not $SkipTests) {
    Write-Host "`n[4/6] Executing unit and integration tests..." -ForegroundColor Yellow
    dotnet test tests/LocalImageConverter.Tests/LocalImageConverter.Tests.csproj -c Release --no-build --logger "console;verbosity=normal"
} else {
    Write-Host "`n[4/6] Tests skipped (--SkipTests)." -ForegroundColor DarkGray
}

# 5. PUBLISH
Write-Host "`n[5/6] Publishing win-x64 self-contained application..." -ForegroundColor Yellow
$PublishDir = Join-Path $DistDir "LocalImageConverter"
dotnet publish src/LocalImageConverter.App/LocalImageConverter.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $PublishDir

$TargetExe = Join-Path $PublishDir "LocalImageConverter.exe"
if (-not (Test-Path $TargetExe)) {
    Write-Error "Publish failed: LocalImageConverter.exe was not found in $PublishDir"
}
Write-Host "Published successfully to $PublishDir" -ForegroundColor Green

# 6. INSTALLER (INNO SETUP)
if (-not $SkipInstaller) {
    Write-Host "`n[6/6] Generating Inno Setup installer..." -ForegroundColor Yellow
    
    # Search for ISCC.exe in standard locations or PATH
    $IsccCandidates = @(
        (Get-Command "iscc.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source),
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:LocalAppData}\Programs\Inno Setup 6\ISCC.exe"
    )

    $IsccPath = $IsccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

    if (-not $IsccPath) {
        Write-Host "Inno Setup compiler (ISCC.exe) not found in standard paths." -ForegroundColor Yellow
        Write-Host "Checking if winget is available to install Inno Setup..." -ForegroundColor Cyan
        
        $WingetCmd = Get-Command "winget" -ErrorAction SilentlyContinue
        if ($WingetCmd) {
            Write-Host "Installing Inno Setup via winget..." -ForegroundColor Cyan
            & winget install JRSoftware.InnoSetup -e --accept-package-agreements --accept-source-agreements --silent
            
            # Recheck path after install
            $IsccPath = $IsccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
        }
    }

    if ($IsccPath -and (Test-Path $IsccPath)) {
        Write-Host "Compiling installer using $IsccPath..." -ForegroundColor Cyan
        $IssScript = Join-Path (Join-Path $ScriptDir "installer") "LocalImageConverter.iss"
        & "$IsccPath" "$IssScript"

        $SetupExe = Join-Path $DistDir "LocalImageConverter-Setup-1.0.0.exe"
        if (Test-Path $SetupExe) {
            Write-Host "`nSUCCESS! Installer created: $SetupExe" -ForegroundColor Green
        } else {
            Write-Host "Installer compilation completed. Check dist folder." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Inno Setup not installed. The standalone application is ready in dist/LocalImageConverter." -ForegroundColor Yellow
        Write-Host "To generate the installer, install Inno Setup 6 from https://jrsoftware.org/isinfo.php and run this script again." -ForegroundColor Yellow
    }
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "                      BUILD FINISHED                      " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Standalone App: $TargetExe"
$FinalSetup = Join-Path $DistDir "LocalImageConverter-Setup-1.0.0.exe"
if (Test-Path $FinalSetup) {
    Write-Host "Setup Installer: $FinalSetup"
}
