$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$version  = $env:chocolateyPackageVersion

$packageArgs = @{
    packageName    = $env:chocolateyPackageName
    unzipLocation  = $toolsDir
    url64bit       = "https://github.com/HueByte/EchoHub/releases/download/v$version/EchoHub-Client-win-x64.zip"
    checksum64     = '__CHECKSUM64__'
    checksumType64 = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs

# Create a shim so 'echohub' is available on PATH
$exeDir = Join-Path $toolsDir 'client-win-x64'
$exePath = Join-Path $exeDir 'EchoHub.Client.exe'
Install-BinFile -Name 'echohub' -Path $exePath
