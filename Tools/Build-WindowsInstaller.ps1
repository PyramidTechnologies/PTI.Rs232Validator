<#
.SYNOPSIS
Builds a Windows installer for PTI.Rs232Validator.Gui.

.PARAMETER BuildConfiguration
The build configuration to use when building binaries.
Valid values are "Debug" and "Release". The default is "Debug".

.PARAMETER ShouldSign
The flag indicating whether every built binaries should be signed.
#>

param(
    [ValidateSet("Debug", "Release")]
    [string] $BuildConfiguration = "Debug",
    [switch] $ShouldSign = $false
)

$ErrorActionPreference = "Stop"

$rootDirectoryPath = Split-Path -Path $PSScriptRoot -Parent
$solutionPath = "$rootDirectoryPath\Rs232Validator.sln"
$guiProjectPath = "$rootDirectoryPath\PTI.Rs232Validator.Gui\PTI.Rs232Validator.Gui.csproj"
$installerProjectPath = "$rootDirectoryPath\PTI.Rs232Validator.Installer\PTI.Rs232Validator.Installer.csproj"

$buildDirectoryPath = "$rootDirectoryPath\Build"
$guiPublishDirectoryPath = "$buildDirectoryPath\PTI.Rs232Validator.Gui"
$guiExePath = "$guiPublishDirectoryPath\PTI.Rs232Validator.Gui.exe"
$initialMsiPath = "$buildDirectoryPath\PTI.Rs232Validator.Installer.msi"
$finalMsiPath = "$buildDirectoryPath\RS232_Validator.msi"

function Sign
{
    param (
        [string] $binaryPath
    )

    Write-Host "`tSigning $binaryPath..." -ForegroundColor Blue
    $signToolPath = "$rootDirectoryPath\Tools\signtool.exe"
    $signArgs = [string]::Format(
            'sign /sha1 {0} /fd sha256 /t http://timestamp.digicert.com /v {1}',
            $env:EV_CERT_ID,
            $binaryPath).Split()
    & $signToolPath $signArgs
    Write-Host "`tOK." -ForegroundColor Green
}

if ($ShouldSign)
{
    if (-not $env:EV_CERT_ID)
    {
        Write-Host "`tDiscovered that the EV_CERT_ID environment variable is not set. Cannot sign binaries." -ForegroundColor Red
        exit 1
    }
}

if (Test-Path $buildDirectoryPath)
{
    Write-Host "Clearing the build directory..." -ForegroundColor Blue
    Remove-Item -Path $buildDirectoryPath -Recurse -Force
    Write-Host "`tOK." -ForegroundColor Green
}

Write-Host "Publishing the GUI application..." -ForegroundColor Blue
dotnet publish /p:DebugType=None /p:DebugSymbols=false $guiProjectPath -o $guiPublishDirectoryPath --self-contained -p:PublishSingleFile=true --framework net8.0-windows --runtime win-x86 --configuration $buildConfiguration -v normal
if (!(Test-Path $guiExePath))
{
    Write-Host "`tFailed to find '$guiExePath' after publishing." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK." -ForegroundColor Green

if ($ShouldSign)
{
    Sign -binaryPath $guiExePath
}

Write-Host "Building the Windows installer..." -ForegroundColor Blue
dotnet run --project $installerProjectPath --configuration $BuildConfiguration -v normal -- -BuildDir $buildDirectoryPath -Binary $guiExePath
if (!(Test-Path $initialMsiPath))
{
    Write-Host "`tFailed to find '$initialMsiPath'." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK." -ForegroundColor Green

Rename-Item -Path $initialMsiPath -NewName $finalMsiPath -Force

if ($ShouldSign)
{
    Sign -binaryPath $finalMsiPath
}

Write-Host "Successfully built the installer at '$finalMsiPath'." -ForegroundColor Green