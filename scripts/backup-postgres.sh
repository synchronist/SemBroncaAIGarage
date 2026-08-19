#!/usr/bin/env sh
set -eu

compose_file="${COMPOSE_FILE:-docker/docker-compose.production.yml}"
env_file="${ENV_FILE:-.env}"
backup_dir="${BACKUP_DIRECTORY:-backups/postgres}"
timestamp="$(date -u +%Y%m%d-%H%M%S)"
backup_file="$backup_dir/sbgarage-$timestamp.dump"
container_file="/tmp/sbgarage-$timestamp.dump"

mkdir -p "$backup_dir"
[ ! -e "$backup_file" ] || { echo "O arquivo de backup já existe." >&2; exit 1; }
[ -n "$(docker compose --env-file "$env_file" -f "$compose_file" ps -q postgres)" ] || { echo "O serviço postgres não está em execução." >&2; exit 1; }

cleanup() {
  docker compose --env-file "$env_file" -f "$compose_file" exec -T postgres rm -f -- "$container_file" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker compose --env-file "$env_file" -f "$compose_file" exec -T postgres sh -ec \
  "pg_dump --username=\"\$POSTGRES_USER\" --dbname=\"\$POSTGRES_DB\" --format=custom --file='$container_file'"
docker compose --env-file "$env_file" -f "$compose_file" cp "postgres:$container_file" "$backup_file"
[ -s "$backup_file" ] || { rm -f -- "$backup_file"; echo "O backup gerado está vazio." >&2; exit 1; }
printf '%s\n' "$backup_file"
