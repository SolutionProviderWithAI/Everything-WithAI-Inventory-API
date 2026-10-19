# ==============================================================================
# Database Backup Script for PostgreSQL (Windows PowerShell)
# ==============================================================================
param(
    [string]$HostName = $env:POSTGRES_HOST ?? "localhost",
    [int]$Port = [int]($env:POSTGRES_PORT ?? 5432),
    [string]$DatabaseName = $env:POSTGRES_DB ?? "inventory_db",
    [string]$UserName = $env:POSTGRES_USER ?? "postgres",
    [string]$BackupDir = ".\backups"
)

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
if (-not (Test-Path -Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

$BackupFile = Join-Path $BackupDir "${DatabaseName}_backup_${Timestamp}.sql"
Write-Host "Creating backup of database '$DatabaseName' on ${HostName}:${Port}..." -ForegroundColor Cyan

$env:PGPASSWORD = $env:POSTGRES_PASSWORD ?? "postgres"

try {
    # If using Docker container
    $dockerRunning = docker ps --filter "name=postgres" --format "{{.Names}}" 2>$null
    if ($dockerRunning -match "postgres") {
        Write-Host "Detected running Docker container 'postgres'. Generating dump via docker exec..." -ForegroundColor Green
        docker exec -t postgres pg_dump -U $UserName -d $DatabaseName > $BackupFile
    } else {
        # Using local pg_dump
        pg_dump -h $HostName -p $Port -U $UserName -F p -b -v -f $BackupFile $DatabaseName
    }

    if (Test-Path $BackupFile) {
        $size = (Get-Item $BackupFile).Length / 1KB
        Write-Host "Backup successfully completed: $BackupFile ($([math]::Round($size, 2)) KB)" -ForegroundColor Green
    } else {
        Write-Error "Backup file was not created."
    }
}
catch {
    Write-Error "Backup failed: $_"
}
