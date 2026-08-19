#!/usr/bin/env sh
set -eu

compose_file="${COMPOSE_FILE:-docker/docker-compose.production.yml}"
env_file="${ENV_FILE:-.env}"
backup_dir="${BACKUP_DIRECTORY:-backups/postgres}"
health_timeout="${HEALTH_TIMEOUT_SECONDS:-180}"
stage="PRECHECK"

log() { printf '[%s] %s\n' "$1" "$2"; }
fail() { printf '[FAILED] Etapa %s: %s\n' "$stage" "$1" >&2; exit 1; }
compose() { docker compose --env-file "$env_file" -f "$compose_file" "$@"; }
container_id() { compose ps -q "$1"; }
wait_healthy() {
  service="$1"; elapsed=0
  while [ "$elapsed" -lt "$health_timeout" ]; do
    id="$(container_id "$service")"
    [ -n "$id" ] || fail "O serviço $service não está em execução."
    status="$(docker inspect "$id" --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}')"
    [ "$status" = healthy ] && return 0
    case "$status" in exited|dead|unhealthy) fail "O serviço $service terminou com estado $status.";; esac
    sleep 2; elapsed=$((elapsed + 2))
  done
  fail "Timeout aguardando health de $service."
}

trap 'code=$?; [ "$code" -eq 0 ] || printf "[FAILED] Etapa %s (código %s).\n" "$stage" "$code" >&2' EXIT
log "$stage" "Validando ambiente."
[ -f "$env_file" ] || fail "Arquivo de ambiente não encontrado: $env_file"
[ -f "$compose_file" ] || fail "Arquivo Compose não encontrado: $compose_file"
[ -f scripts/backup-postgres.sh ] || fail "Script de backup não encontrado."
command -v docker >/dev/null 2>&1 || fail "Docker não encontrado."
docker info >/dev/null 2>&1 || fail "Docker indisponível."
docker compose version >/dev/null 2>&1 || fail "Docker Compose indisponível."
compose config --quiet || fail "Configuração Compose inválida."
services="$(compose --profile tools config --services)"
for required in postgres api web migrate; do printf '%s\n' "$services" | grep -qx "$required" || fail "Serviço ausente: $required"; done
wait_healthy postgres
mkdir -p "$backup_dir"
[ -w "$backup_dir" ] || fail "Diretório de backup sem permissão de escrita."
available_kb="$(df -Pk "$backup_dir" | awk 'NR==2 {print $4}')"
[ "${available_kb:-0}" -ge 1048576 ] || fail "Espaço livre inferior a 1 GB no destino de backup."

revision="$(git rev-parse --short=12 HEAD 2>/dev/null || printf local)"
[ -z "$(git status --porcelain 2>/dev/null || true)" ] || revision="$revision-dirty"
export APP_VERSION="$revision"
current_id="$(container_id api)"; [ -n "$current_id" ] || fail "API atual não está em execução."
current_version="$(docker inspect "$current_id" --format '{{index .Config.Labels "org.opencontainers.image.revision"}}' 2>/dev/null || true)"
log "$stage" "Versão atual: ${current_version:-unknown}; candidata: $revision."

stage="BACKUP"; log "$stage" "Criando backup obrigatório."
backup_path="$(COMPOSE_FILE="$compose_file" ENV_FILE="$env_file" BACKUP_DIRECTORY="$backup_dir" sh scripts/backup-postgres.sh | tail -n 1)"
[ -s "$backup_path" ] || fail "Backup obrigatório inválido."
validate_path="/tmp/sbgarage-deploy-validate-$$.dump"
cleanup_validate() { compose exec -T postgres rm -f -- "$validate_path" >/dev/null 2>&1 || true; }
trap 'cleanup_validate; code=$?; [ "$code" -eq 0 ] || printf "[FAILED] Etapa %s (código %s).\n" "$stage" "$code" >&2' EXIT
compose cp "$backup_path" "postgres:$validate_path" || fail "Falha ao copiar backup para validação."
compose exec -T postgres sh -ec "test \$(pg_restore --list '$validate_path' | wc -l) -gt 0" || fail "pg_restore rejeitou o backup."
cleanup_validate
log "$stage" "Backup validado: $(basename "$backup_path")."

stage="BUILD"; log "$stage" "Preparando imagens."
compose --profile tools build api web migrate || fail "Build das imagens falhou."
stage="MIGRATION"; log "$stage" "Executando migrations explícitas."
compose --profile tools run --rm migrate || fail "Migration falhou; aplicação atual preservada."
stage="UPDATE"; log "$stage" "Atualizando API."
compose up -d --no-deps api || fail "Update da API falhou."
stage="HEALTH"; wait_healthy api
stage="UPDATE"; log "$stage" "Atualizando Web."
compose up -d --no-deps web || fail "Update do Web falhou."
stage="HEALTH"; log "$stage" "Aguardando PostgreSQL, API e Web saudáveis."
wait_healthy postgres; wait_healthy api; wait_healthy web
compose exec -T api curl --fail --silent --show-error http://localhost:8080/health/ready >/dev/null || fail "API readiness interno falhou."
base_url="$(sed -n 's/^APP_PUBLIC_BASE_URL=//p' "$env_file" | tail -n 1 | sed 's:/*$::')"
[ -n "$base_url" ] || fail "APP_PUBLIC_BASE_URL não configurada."
curl --fail --silent --show-error "$base_url/health/live" >/dev/null || fail "Web liveness falhou."
curl --fail --silent --show-error "$base_url/health/ready" >/dev/null || fail "Web readiness falhou."
log SUCCESS "Deploy $revision concluído; backup $(basename "$backup_path")."
