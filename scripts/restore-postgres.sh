#!/usr/bin/env sh
set -eu

[ "$#" -eq 2 ] || { echo "Uso: $0 BACKUP_FILE TARGET_DATABASE" >&2; exit 2; }
backup_file="$1"
target_database="$2"
compose_file="${COMPOSE_FILE:-docker/docker-compose.production.yml}"
env_file="${ENV_FILE:-.env}"
service="${POSTGRES_SERVICE:-postgres}"

[ -s "$backup_file" ] || { echo "Backup inexistente ou vazio." >&2; exit 1; }
case "$target_database" in (*[!A-Za-z0-9_-]*|'') echo "Nome de banco de destino inválido." >&2; exit 1;; esac
source_database="$(sed -n 's/^POSTGRES_DB=//p' "$env_file" | tail -n 1)"
[ -n "$source_database" ] || { echo "POSTGRES_DB não foi encontrado." >&2; exit 1; }
[ "$target_database" != "$source_database" ] || { echo "Restauração sobre o banco principal é proibida." >&2; exit 1; }
[ -n "$(docker compose --env-file "$env_file" -f "$compose_file" ps -q "$service")" ] || { echo "O serviço PostgreSQL de destino não está em execução." >&2; exit 1; }

container_file="/tmp/sbgarage-restore-$$.dump"
cleanup() {
  docker compose --env-file "$env_file" -f "$compose_file" exec -T "$service" rm -f -- "$container_file" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker compose --env-file "$env_file" -f "$compose_file" cp "$backup_file" "$service:$container_file"
docker compose --env-file "$env_file" -f "$compose_file" exec -T "$service" sh -ec \
  "if psql --username=\"\$POSTGRES_USER\" --dbname=postgres --tuples-only --no-align --command=\"SELECT 1 FROM pg_database WHERE datname = '$target_database'\" | grep -q 1; then echo 'O banco de destino já existe.' >&2; exit 20; fi; createdb --username=\"\$POSTGRES_USER\" '$target_database'; pg_restore --exit-on-error --no-owner --no-privileges --username=\"\$POSTGRES_USER\" --dbname='$target_database' '$container_file'"
