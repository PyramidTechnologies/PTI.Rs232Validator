<#
.SYNOPSIS
Publishes PTI.Rs232Validator.Cli and PTI.Rs232Validator.Desktop.

.PARAMETER IsRelease
A flag indicating whether the apps should be published with the release configuration.
#>

param(
    [switch] $IsRelease = $true,
    [switch] $ShouldSign = $true
)

$ErrorActionPreference = "Stop"

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

$signToolPath = "$currentDirectoryPath\Tools\signtool.exe"

function Publish
{
    param (
        [string] $projectPath,
        [string] $framework,
        [string] $publishDirectoryPath,
        [string] $binaryPath
    )

    Write-Host "Publishing $projectPath in $publishDirectoryPath..."
    dotnet publish /p:DebugType=None /p:DebugSymbols=false $projectPath -o $publishDirectoryPath --self-contained -p:PublishSingleFile=true --framework $framework --runtime win-x86 --configuration $buildConfiguration -v normal
    if (!(Test-Path $binaryPath))
    {
        Write-Host "`tFailed to publish $projectPath." -ForegroundColor Red
        exit 1
    }
    Write-Host "`tOK" -ForegroundColor Green

    if ($ShouldSign)
    {
        Write-Host "Signing $binaryPath..."
        $signArgs = [string]::Format(
                'sign /sha1 {0} /fd sha256 /t http://timestamp.digicert.com /v {1}',
                $env:EV_CERT_ID,
                $binaryPath).Split()
        & $signToolPath $signArgs

        Write-Host "`tOK." -ForegroundColor Green
    }
}

Write-Host "Checking that this script is invoked from the root of the repository..."
if (!(Test-Path $solutionPath))
{
    Write-Host "This script was not invoked from the root of the repository." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Checking that the EV_CERT_ID environment variable is set..."
if (-not $env:EV_CERT_ID)
{
    Write-Host "The EV_CERT_ID environment variable is not set." -ForegroundColor Red
    exit 1
}

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

Publish $cliProjectPath net8.0 $cliPublishDirectoryPath $cliBinaryPath
Publish $desktopProjectPath net8.0-windows $desktopPublishDirectoryPath $desktopBinaryPath
exit 0