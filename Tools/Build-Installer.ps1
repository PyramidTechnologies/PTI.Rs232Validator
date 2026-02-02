$currentDirectoryPath = Get-Location
$buildDirectoryPath = "$currentDirectoryPath\Build"
$desktopPublishDirectoryPath = "$buildDirectoryPath\PTI.Rs232Validator.Desktop"
$desktopBinaryPath = "$desktopPublishDirectoryPath\PTI.Rs232Validator.Desktop.exe"

Write-Host "Publishing desktop application..."
Invoke-Expression "& `"$PSScriptRoot/Publish-Apps.ps1`" -IsRelease -ShouldSign"

if (!(Test-Path $desktopBinaryPath)) {
    Write-Host "`tFailed to find published desktop binary." -ForegroundColor Red
    exit 1
}

Write-Host "Building installer..."
$installerProjectPath = "$currentDirectoryPath\PTI.Rs232Validator.Installer\PTI.Rs232Validator.Installer.csproj"
dotnet run --project $installerProjectPath --configuration Release -v normal -- -BuildDir $buildDirectoryPath -Binary $desktopBinaryPath 

$installerFilePath = "$buildDirectoryPath\PTI.Rs232Validator.Installer.msi"
if (!(Test-Path $installerFilePath)) {
    Write-Host "`tFailed to build installer." -ForegroundColor Red
    exit 1
}

Write-Host "`tSigning installer..." -ForegroundColor Green
$signToolPath = "$currentDirectoryPath\Tools\signtool.exe"
$signArgs = [string]::Format(
        'sign /sha1 {0} /fd sha256 /t http://timestamp.digicert.com /v {1}',
        $env:EV_CERT_ID,
        $installerFilePath).Split()
& $signToolPath $signArgs

Write-Host "`tOK." -ForegroundColor Green
Write-Host "Installer built successfully: $installerFilePath" -ForegroundColor Green