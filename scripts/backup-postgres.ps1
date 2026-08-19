[CmdletBinding()]
param(
    [string]$ComposeFile = "docker/docker-compose.production.yml",
    [string]$EnvFile = ".env",
    [string]$BackupDirectory = "backups/postgres"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $BackupDirectory))
$backupFile = Join-Path $backupRoot "sbgarage-$timestamp.dump"
$containerFile = "/tmp/sbgarage-$timestamp.dump"

New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
if (Test-Path -LiteralPath $backupFile) {
    throw "O arquivo de backup já existe: $backupFile"
}

$containerId = docker compose --env-file $EnvFile -f $ComposeFile ps -q postgres
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerId)) {
    throw "O serviço postgres não está em execução."
}

try {
    docker compose --env-file $EnvFile -f $ComposeFile exec -T postgres sh -ec `
        "pg_dump --username=`"`$POSTGRES_USER`" --dbname=`"`$POSTGRES_DB`" --format=custom --file='$containerFile'"
    if ($LASTEXITCODE -ne 0) { throw "pg_dump falhou." }

    docker compose --env-file $EnvFile -f $ComposeFile cp "postgres:$containerFile" $backupFile
    if ($LASTEXITCODE -ne 0) { throw "Não foi possível copiar o backup para o host." }
    if ((Get-Item -LiteralPath $backupFile).Length -le 0) { throw "O backup gerado está vazio." }

    Write-Output $backupFile
}
catch {
    if (Test-Path -LiteralPath $backupFile) { Remove-Item -LiteralPath $backupFile -Force }
    throw
}
finally {
    docker compose --env-file $EnvFile -f $ComposeFile exec -T postgres rm -f -- $containerFile 2>$null
}
