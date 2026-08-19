# Backup e restauração do PostgreSQL

O ambiente usa backup lógico oficial do PostgreSQL em formato custom (`pg_dump -Fc`). O dump é portável, pode ser inspecionado com `pg_restore` e não depende de copiar o volume enquanto o servidor está ativo.

## Criar e validar um backup

No Windows, a partir da raiz do repositório:

```powershell
./scripts/backup-postgres.ps1
```

No Linux/VPS:

```sh
sh ./scripts/backup-postgres.sh
```

Os arquivos são gravados em `backups/postgres/sbgarage-YYYYMMDD-HHmmss.dump`. Todo o diretório `backups/` é ignorado pelo Git. Para listar os backups use `Get-ChildItem backups/postgres` no PowerShell ou `ls -lh backups/postgres` no Linux.

Valide um dump sem restaurá-lo:

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml cp ./backups/postgres/ARQUIVO.dump postgres:/tmp/validate.dump
docker compose --env-file .env -f docker/docker-compose.production.yml exec -T postgres pg_restore --list /tmp/validate.dump
docker compose --env-file .env -f docker/docker-compose.production.yml exec -T postgres rm -f /tmp/validate.dump
```

O conteúdo listado contém metadados de estrutura; não publique essa saída nem o dump.

## Restaurar somente em banco alternativo

O destino é obrigatório. Os scripts recusam o nome definido em `POSTGRES_DB` e também recusam sobrescrever um banco de destino já existente:

```powershell
./scripts/restore-postgres.ps1 -BackupFile ./backups/postgres/ARQUIVO.dump -TargetDatabase sbgarage_restore_test
```

```sh
sh ./scripts/restore-postgres.sh ./backups/postgres/ARQUIVO.dump sbgarage_restore_test
```

Para um ensaio com isolamento máximo, use um container PostgreSQL 17 temporário e um Compose/arquivo de ambiente dedicado a ele. Nunca aponte um teste destrutivo para o volume principal.

## Estratégia para a VPS

Na VPS única, o script Linux poderá ser chamado diariamente pelo `cron`. A operação só será considerada protegida quando houver também cópia automática para fora da VPS, retenção definida (por exemplo, diários, semanais e mensais), monitoramento de falhas e teste periódico de restauração em destino isolado. Nunca confie em um backup que não tenha passado por restore real.

Armazenamento externo, criptografia externa, PITR e WAL archiving ficam fora desta etapa.
