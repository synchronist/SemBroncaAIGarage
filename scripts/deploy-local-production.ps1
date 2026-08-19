[CmdletBinding()]
param(
    [string]$ComposeFile = "docker/docker-compose.production.yml",
    [string]$EnvFile = ".env",
    [string]$BackupDirectory = "backups/postgres",
    [int]$HealthTimeoutSeconds = 180
)

$ErrorActionPreference = "Stop"
$stage = "PRECHECK"
$repositoryRoot = [System.IO.Path]::GetFullPath((Get-Location).Path)

function Write-Stage([string]$Name, [string]$Message) {
    Write-Host "[$Name] $Message"
}

function Invoke-Checked([string]$Description, [scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Description falhou (código $LASTEXITCODE)." }
}

function Get-ServiceContainer([string]$Service) {
    $id = docker compose --env-file $EnvFile -f $ComposeFile ps -q $Service
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($id)) { throw "O serviço $Service não está em execução." }
    return $id.Trim()
}

function Wait-ServiceHealthy([string]$Service) {
    $deadline = [DateTime]::UtcNow.AddSeconds($HealthTimeoutSeconds)
    do {
        $id = Get-ServiceContainer $Service
        $status = docker inspect $id --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}'
        if ($LASTEXITCODE -ne 0) { throw "Não foi possível consultar o health de $Service." }
        if ($status.Trim() -eq "healthy") { return }
        if ($status.Trim() -in @("exited", "dead", "unhealthy")) { throw "O serviço $Service terminou com estado $($status.Trim())." }
        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Timeout aguardando health de $Service."
}

try {
    Write-Stage $stage "Validando ambiente."
    if (-not (Test-Path -LiteralPath $EnvFile -PathType Leaf)) { throw "Arquivo de ambiente não encontrado: $EnvFile" }
    if (-not (Test-Path -LiteralPath $ComposeFile -PathType Leaf)) { throw "Arquivo Compose não encontrado: $ComposeFile" }
    if (-not (Test-Path -LiteralPath "scripts/backup-postgres.ps1" -PathType Leaf)) { throw "Script de backup não encontrado." }
    Invoke-Checked "Docker" { docker info --format '{{.ServerVersion}}' | Out-Null }
    Invoke-Checked "Docker Compose" { docker compose version | Out-Null }
    Invoke-Checked "Configuração Compose" { docker compose --env-file $EnvFile -f $ComposeFile config --quiet }
    $services = docker compose --env-file $EnvFile -f $ComposeFile --profile tools config --services
    if ($LASTEXITCODE -ne 0 -or @("postgres", "api", "web", "migrate").Where({ $_ -notin $services }).Count -gt 0) {
        throw "O Compose não contém todos os serviços esperados."
    }
    Wait-ServiceHealthy "postgres"
    $backupRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $BackupDirectory))
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    $drive = Get-PSDrive -Name ([System.IO.Path]::GetPathRoot($backupRoot).Substring(0, 1))
    if ($drive.Free -lt 1GB) { throw "Espaço livre inferior a 1 GB no destino de backup." }
    $probe = Join-Path $backupRoot ".deploy-write-$PID.tmp"
    try { [System.IO.File]::WriteAllText($probe, "ok") } finally { if (Test-Path -LiteralPath $probe) { Remove-Item -LiteralPath $probe -Force } }

    $revision = (git rev-parse --short=12 HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($revision)) { $revision = "local" }
    if (-not [string]::IsNullOrWhiteSpace((git status --porcelain 2>$null))) { $revision = "$revision-dirty" }
    $env:APP_VERSION = $revision
    $currentApi = Get-ServiceContainer "api"
    $currentVersion = docker inspect $currentApi --format '{{index .Config.Labels "org.opencontainers.image.revision"}}'
    if ([string]::IsNullOrWhiteSpace($currentVersion)) { $currentVersion = "unknown" }
    Write-Stage $stage "Versão atual: $currentVersion; candidata: $revision."

    $stage = "BACKUP"
    Write-Stage $stage "Criando backup obrigatório."
    $backupOutput = & "scripts/backup-postgres.ps1" -ComposeFile $ComposeFile -EnvFile $EnvFile -BackupDirectory $BackupDirectory
    if ($LASTEXITCODE -ne 0) { throw "O backup obrigatório falhou." }
    $backupPath = [string]($backupOutput | Select-Object -Last 1)
    if ([string]::IsNullOrWhiteSpace($backupPath)) { throw "O script não informou o arquivo de backup." }
    $backup = Get-Item -LiteralPath $backupPath -ErrorAction Stop
    if ($backup.Length -le 0 -or -not $backup.FullName.StartsWith($backupRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "O backup obrigatório é inválido."
    }
    $validatePath = "/tmp/sbgarage-deploy-validate-$PID.dump"
    try {
        Invoke-Checked "Cópia do backup para validação" { docker compose --env-file $EnvFile -f $ComposeFile cp $backup.FullName "postgres:$validatePath" }
        Invoke-Checked "Validação pg_restore" { docker compose --env-file $EnvFile -f $ComposeFile exec -T postgres sh -ec "test `$(pg_restore --list '$validatePath' | wc -l) -gt 0" }
    }
    finally {
        docker compose --env-file $EnvFile -f $ComposeFile exec -T postgres rm -f -- $validatePath 2>$null | Out-Null
    }
    Write-Stage $stage "Backup validado: $($backup.Name)."

    $stage = "BUILD"
    Write-Stage $stage "Construindo imagens com cache normal."
    Invoke-Checked "Build das imagens" { docker compose --env-file $EnvFile -f $ComposeFile --profile tools build api web migrate }

    $stage = "MIGRATION"
    Write-Stage $stage "Executando migrations explícitas."
    Invoke-Checked "Migrations" { docker compose --env-file $EnvFile -f $ComposeFile --profile tools run --rm migrate }

    $stage = "UPDATE"
    Write-Stage $stage "Atualizando API."
    Invoke-Checked "Update da API" { docker compose --env-file $EnvFile -f $ComposeFile up -d --no-deps api }
    $stage = "HEALTH"
    Wait-ServiceHealthy "api"
    $stage = "UPDATE"
    Write-Stage $stage "Atualizando Web."
    Invoke-Checked "Update do Web" { docker compose --env-file $EnvFile -f $ComposeFile up -d --no-deps web }

    $stage = "HEALTH"
    Write-Stage $stage "Aguardando PostgreSQL, API e Web saudáveis."
    Wait-ServiceHealthy "postgres"; Wait-ServiceHealthy "api"; Wait-ServiceHealthy "web"
    Invoke-Checked "API readiness interno" { docker compose --env-file $EnvFile -f $ComposeFile exec -T api curl --fail --silent --show-error http://localhost:8080/health/ready | Out-Null }
    $baseUrlLine = Get-Content -LiteralPath $EnvFile | Where-Object { $_ -match '^APP_PUBLIC_BASE_URL=' } | Select-Object -Last 1
    $baseUrl = (($baseUrlLine -split '=', 2)[1]).Trim().TrimEnd('/')
    if ([string]::IsNullOrWhiteSpace($baseUrl)) { throw "APP_PUBLIC_BASE_URL não configurada." }
    Invoke-WebRequest -UseBasicParsing -Uri "$baseUrl/health/live" -TimeoutSec 15 | Out-Null
    Invoke-WebRequest -UseBasicParsing -Uri "$baseUrl/health/ready" -TimeoutSec 15 | Out-Null

    Write-Stage "SUCCESS" "Deploy $revision concluído; backup $($backup.Name)."
}
catch {
    Write-Error "[FAILED] Etapa ${stage}: $($_.Exception.Message)"
    exit 1
}
finally {
    Remove-Item Env:APP_VERSION -ErrorAction SilentlyContinue
}
