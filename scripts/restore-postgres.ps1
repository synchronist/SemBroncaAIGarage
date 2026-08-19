[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$BackupFile,
    [Parameter(Mandatory)] [string]$TargetDatabase,
    [string]$ComposeFile = "docker/docker-compose.production.yml",
    [string]$EnvFile = ".env",
    [string]$Service = "postgres"
)

$ErrorActionPreference = "Stop"
$resolvedBackup = (Resolve-Path -LiteralPath $BackupFile -ErrorAction Stop).Path
if ((Get-Item -LiteralPath $resolvedBackup).Length -le 0) { throw "O arquivo de backup está vazio." }
if ($TargetDatabase -notmatch '^[A-Za-z_][A-Za-z0-9_-]*$') { throw "Nome de banco de destino inválido." }

$sourceDatabaseLine = Get-Content -LiteralPath $EnvFile | Where-Object { $_ -match '^POSTGRES_DB=' } | Select-Object -Last 1
if ([string]::IsNullOrWhiteSpace($sourceDatabaseLine)) { throw "POSTGRES_DB não foi encontrado no arquivo de ambiente." }
$sourceDatabase = ($sourceDatabaseLine -split '=', 2)[1].Trim()
if ($TargetDatabase -eq $sourceDatabase) {
    throw "Restauração sobre o banco principal é proibida. Informe outro banco de destino."
}

$containerId = docker compose --env-file $EnvFile -f $ComposeFile ps -q $Service
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerId)) { throw "O serviço PostgreSQL de destino não está em execução." }
$containerFile = "/tmp/sbgarage-restore-$([Guid]::NewGuid().ToString('N')).dump"

try {
    docker compose --env-file $EnvFile -f $ComposeFile cp $resolvedBackup "${Service}:$containerFile"
    if ($LASTEXITCODE -ne 0) { throw "Não foi possível copiar o backup para o destino." }

    docker compose --env-file $EnvFile -f $ComposeFile exec -T $Service sh -ec `
        "if psql --username=`"`$POSTGRES_USER`" --dbname=postgres --tuples-only --no-align --command=`"SELECT 1 FROM pg_database WHERE datname = '$TargetDatabase'`" | grep -q 1; then echo 'O banco de destino já existe.' >&2; exit 20; fi; createdb --username=`"`$POSTGRES_USER`" '$TargetDatabase'; pg_restore --exit-on-error --no-owner --no-privileges --username=`"`$POSTGRES_USER`" --dbname='$TargetDatabase' '$containerFile'"
    if ($LASTEXITCODE -ne 0) { throw "A restauração falhou; revise o banco de destino isolado." }
}
finally {
    docker compose --env-file $EnvFile -f $ComposeFile exec -T $Service rm -f -- $containerFile 2>$null
}
