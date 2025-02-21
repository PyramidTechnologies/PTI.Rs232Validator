<#
.SYNOPSIS
Publishes PTI.Rs232Validator.Cli and PTI.Rs232Validator.Desktop.

.PARAMETER IsRelease
A flag indicating whether the apps should be published with the release configuration.
#>

param(
    [switch] $IsRelease = $true
)

$buildConfiguration = "Debug"
if ($IsRelease)
{
    $buildConfiguration = "Release"
}

$currentDirectoryPath = Get-Location
$solutionPath = "$currentDirectoryPath\Rs232Validator.sln"
$cliProjectPath = "$currentDirectoryPath\PTI.Rs232Validator.Cli\PTI.Rs232Validator.Cli.csproj"
$desktopProjectPath = "$currentDirectoryPath\PTI.Rs232Validator.Desktop\PTI.Rs232Validator.Desktop.csproj"

$buildDirectoryPath = "$currentDirectoryPath\Build"
$cliPublishDirectoryPath = "$buildDirectoryPath\PTI.Rs232Validator.Cli"
$cliBinaryPath = "$cliPublishDirectoryPath\PTI.Rs232Validator.Cli.exe"
$desktopPublishDirectoryPath = "$buildDirectoryPath\PTI.Rs232Validator.Desktop"
$desktopBinaryPath = "$desktopPublishDirectoryPath\PTI.Rs232Validator.Desktop.exe"

Write-Host "Checking that this script is invoked from the root of the repository..."
if (!(Test-Path $solutionPath))
{
    Write-Host "This script must be invoked from the root of the repository." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Clearing out the build directory..."
if (Test-Path $buildDirectoryPath)
{
    Remove-Item -Recurse -Force -Path "$buildDirectoryPath\*"
}
else
{
    New-Item -ItemType Directory -Path $buildDirectoryPath
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Publishing the CLI project..."
dotnet publish /p:DebugType=None /p:DebugSymbols=false $cliProjectPath -o $cliPublishDirectoryPath --self-contained -p:PublishSingleFile=true --framework net8.0 --runtime win-x86 --configuration $buildConfiguration -v normal
if (!$? -or !(Test-Path $cliBinaryPath))
{
    Write-Host "Failed to find $cliBinaryPath." -ForegroundColor Red
    exit 1
}

Write-Host "Publishing the desktop project..."
dotnet publish /p:DebugType=None /p:DebugSymbols=false $desktopProjectPath -o $desktopPublishDirectoryPath --self-contained -p:PublishSingleFile=true --framework net8.0-windows --runtime win-x86 --configuration $buildConfiguration -v normal
if (!$? -or !(Test-Path $desktopBinaryPath))
{
    Write-Host "Failed to find $desktopBinaryPath." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

exit 0