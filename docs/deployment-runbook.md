# Runbook de deploy, atualização e rollback

> **Nunca execute `docker compose down -v` em produção.** Não remova volumes de PostgreSQL, Data Protection, logos ou backups durante deploy e rollback.

## Pré-deploy

Trabalhe a partir da raiz do checkout que será implantado. Confirme `.env`, Docker/Compose, espaço em disco, stack saudável e backup externo recente. O script registra a revisão com o Git SHA curto; uma árvore com alterações locais recebe o sufixo `-dirty`. A mesma identificação fica na label `org.opencontainers.image.revision` das imagens e containers.

Windows/LocalProduction:

```powershell
./scripts/deploy-local-production.ps1
```

Linux/VPS:

```sh
sh ./scripts/deploy-production.sh
```

## Fluxo do deploy

Os scripts executam `PRECHECK → BACKUP → BUILD → MIGRATION → UPDATE → HEALTH → SUCCESS`. Qualquer falha gera `[FAILED]` com a etapa e interrompe as etapas seguintes.

O backup `pg_dump -Fc` é obrigatório, salvo em `backups/postgres/` e validado por `pg_restore --list`. Em seguida, as imagens são construídas com cache normal. Para futura distribuição por registry, substitua a etapa de build por pull de imagens imutáveis mantendo as demais gates.

As migrations são executadas apenas por:

```sh
docker compose --env-file .env -f docker/docker-compose.production.yml --profile tools run --rm migrate
```

Elas nunca rodam no startup. Falha de migration preserva os containers atuais e não dispara rollback automático de schema.

Depois da migration, API e Web são atualizados separadamente. O deploy só termina quando PostgreSQL, API e Web estão `healthy`, o readiness interno da API responde e `/health/live` e `/health/ready` do Web passam dentro do timeout.

## Comandos manuais de emergência

```sh
docker compose --env-file .env -f docker/docker-compose.production.yml config --quiet
sh scripts/backup-postgres.sh
docker compose --env-file .env -f docker/docker-compose.production.yml --profile tools build api web migrate
docker compose --env-file .env -f docker/docker-compose.production.yml --profile tools run --rm migrate
docker compose --env-file .env -f docker/docker-compose.production.yml up -d --no-deps api
docker compose --env-file .env -f docker/docker-compose.production.yml up -d --no-deps web
docker compose --env-file .env -f docker/docker-compose.production.yml ps
curl --fail http://localhost:8080/health/live
curl --fail http://localhost:8080/health/ready
```

## Falha e rollback da aplicação

Reúna `docker compose ... ps` e `docker compose ... logs --tail=200 api web migrate`. Não restaure banco automaticamente por falha de health.

Se o schema continuar compatível, volte o checkout para o Git SHA anterior ou selecione as imagens imutáveis da versão anterior, defina `APP_VERSION` com essa revisão, reconstrua/puxe apenas API e Web, execute `up -d --no-deps api web` e repita todo o health gate. Não execute migrations antigas para “desfazer” schema sem análise específica.

## Rollback de banco e restore de desastre

Restore do banco principal é uma operação excepcional e deliberadamente não automatizada. Antes dela:

1. interrompa API e Web sem remover volumes;
2. gere e preserve um backup do estado atual;
3. escolha um dump conhecido e já testado;
4. confirme a compatibilidade entre aplicação e schema;
5. restaure em ambiente isolado e valide primeiro;
6. somente com autorização explícita, planeje a substituição do banco principal;
7. suba a versão compatível da aplicação e execute health e smoke checks.

Consulte [Backup e restauração](backup-restore.md). Uma migration aplicada pode conter transformação irreversível; rollback de aplicação e rollback de banco são decisões separadas.
