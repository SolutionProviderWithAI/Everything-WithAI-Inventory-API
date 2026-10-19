# ==============================================================================
# Database Restore Script for PostgreSQL (Windows PowerShell)
# ==============================================================================
param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    [string]$HostName = $env:POSTGRES_HOST ?? "localhost",
    [int]$Port = [int]($env:POSTGRES_PORT ?? 5432),
    [string]$DatabaseName = $env:POSTGRES_DB ?? "inventory_db",
    [string]$UserName = $env:POSTGRES_USER ?? "postgres"
)

if (-not (Test-Path $BackupFile)) {
    Write-Error "Backup file '$BackupFile' does not exist."
    exit 1
}

Write-Host "Restoring database '$DatabaseName' from $BackupFile..." -ForegroundColor Cyan
$env:PGPASSWORD = $env:POSTGRES_PASSWORD ?? "postgres"

try {
    $dockerRunning = docker ps --filter "name=postgres" --format "{{.Names}}" 2>$null
    if ($dockerRunning -match "postgres") {
        Write-Host "Detected running Docker container 'postgres'. Restoring via docker exec..." -ForegroundColor Green
        Get-Content $BackupFile | docker exec -i postgres psql -U $UserName -d $DatabaseName
    } else {
        psql -h $HostName -p $Port -U $UserName -d $DatabaseName -f $BackupFile
    }
    Write-Host "Database restore completed successfully." -ForegroundColor Green
}
catch {
    Write-Error "Database restore failed: $_"
}
