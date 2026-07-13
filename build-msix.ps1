# Construye el paquete MSIX de Dudiver Music (Windows).
# Uso:   .\build-msix.ps1              (x64, sin firmar -> para subir a la Store)
#        .\build-msix.ps1 -Platform arm64
#        .\build-msix.ps1 -Sign        (firma con cert de prueba para instalar/probar local)
param(
  [ValidateSet('x64','arm64')] [string]$Platform = 'x64',
  [switch]$Sign
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$outDir = Join-Path $root '_msix'

# Ubicar MSBuild de Visual Studio
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) { throw "No encontré vswhere. Instalá Visual Studio (con 'Desarrollo de escritorio .NET' + 'Herramientas de empaquetado MSIX')." }
$msbuild = & $vswhere -latest -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
if (-not $msbuild) { throw "No encontré MSBuild.exe." }

Write-Host "MSBuild: $msbuild"
Write-Host "Plataforma: $Platform`n"

& $msbuild "$root\DudiverMusic.Package\DudiverMusic.Package.wapproj" `
  /restore `
  /p:Configuration=Release `
  /p:Platform=$Platform `
  /p:UapAppxPackageBuildMode=SideloadOnly `
  /p:AppxBundle=Never `
  /p:AppxPackageSigningEnabled=false `
  /p:AppxPackageDir="$outDir\" `
  /m /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "El build del MSIX falló (código $LASTEXITCODE)." }

$msix = Get-ChildItem -Path $outDir -Recurse -Filter *.msix | Sort-Object LastWriteTime | Select-Object -Last 1
Write-Host "`nMSIX generado: $($msix.FullName)"
Write-Host ("Tamaño: {0:N1} MB" -f ($msix.Length / 1MB))

if ($Sign) {
  # Firma con un certificado de PRUEBA (solo para instalar/probar en tu PC, NO para la Store).
  # La Store firma el paquete por vos al publicar.
  $pfx = Join-Path $root '_msix\DudiverMusic_Test.pfx'
  $pwd = ConvertTo-SecureString 'dudiver' -AsPlainText -Force
  if (-not (Test-Path $pfx)) {
    Write-Host "`nCreando certificado de prueba (CN=Roberth Dudiver)..."
    $cert = New-SelfSignedCertificate -Type Custom -Subject "CN=Roberth Dudiver" `
      -KeyUsage DigitalSignature -FriendlyName "Dudiver Music Test" `
      -CertStoreLocation "Cert:\CurrentUser\My" `
      -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $pwd | Out-Null
    Write-Host "Cert de prueba: $pfx (para confiar en él: Import-PfxCertificate ... Cert:\LocalMachine\Root, requiere admin)"
  }
  $signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe |
    Where-Object { $_.FullName -match 'x64' } | Sort-Object FullName | Select-Object -Last 1
  & $signtool.FullName sign /fd SHA256 /a /f $pfx /p 'dudiver' $msix.FullName
  Write-Host "`nMSIX firmado (prueba). Para instalar: hacé doble clic en el .msix (antes confiá en el cert)."
}

Write-Host "`nPara la Store: subí este .msix en Partner Center (Microsoft lo firma). Ver docs/store-submission-guide.md"
