# PowerShell script para deployment en IIS
# Ejecutar como Administrador

param(
    [string]$SiteName = "FinancialAnalytics",
    [string]$AppPoolName = "FinancialAnalyticsPool",
    [string]$Port = "5000",
    [string]$PhysicalPath = "C:\inetpub\wwwroot\FinancialAnalytics"
)

Write-Host "=== Financial Analytics - Despliegue en IIS ===" -ForegroundColor Green

# 1. Publicar la aplicación
Write-Host "`n[1/5] Publicando aplicación..." -ForegroundColor Yellow
Set-Location -Path "$PSScriptRoot\FinancialAnalytics.API"
dotnet publish -c Release -o "$PSScriptRoot\publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Falló la publicación de la aplicación" -ForegroundColor Red
    exit 1
}

# 2. Crear directorio físico
Write-Host "`n[2/5] Creando directorio físico..." -ForegroundColor Yellow
if (!(Test-Path $PhysicalPath)) {
    New-Item -ItemType Directory -Path $PhysicalPath -Force
}

# 3. Copiar archivos publicados
Write-Host "`n[3/5] Copiando archivos publicados..." -ForegroundColor Yellow
Copy-Item -Path "$PSScriptRoot\publish\*" -Destination $PhysicalPath -Recurse -Force

# 4. Crear Application Pool
Write-Host "`n[4/5] Configurando IIS Application Pool..." -ForegroundColor Yellow
Import-Module WebAdministration

if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "Application Pool ya existe, eliminando..." -ForegroundColor Yellow
    Remove-WebAppPool -Name $AppPoolName
}

New-WebAppPool -Name $AppPoolName
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "enable32BitAppOnWin64" -Value $false

# 5. Crear o actualizar sitio web
Write-Host "`n[5/5] Configurando Sitio Web IIS..." -ForegroundColor Yellow

if (Test-Path "IIS:\Sites\$SiteName") {
    Write-Host "Sitio web ya existe, eliminando..." -ForegroundColor Yellow
    Remove-Website -Name $SiteName
}

New-Website -Name $SiteName `
    -PhysicalPath $PhysicalPath `
    -ApplicationPool $AppPoolName `
    -Port $Port

Write-Host "`n=== Despliegue Completado ===" -ForegroundColor Green
Write-Host "URL del Sitio: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "URL de Swagger: http://localhost:$Port/swagger" -ForegroundColor Cyan
Write-Host "`nNota: Asegúrese de actualizar la cadena de conexión MySQL en appsettings.json" -ForegroundColor Yellow
