# Produção local com Docker Compose

Este ambiente empacota PostgreSQL 17, API e Web em modo `Production`. Ele não configura domínio, TLS ou proxy reverso. Somente a porta `8080` do Web é publicada; API e PostgreSQL permanecem nas redes internas do Compose.

## 1. Preparar a configuração

Na raiz do repositório, copie `.env.example` para `.env` e substitua todos os placeholders por valores locais seguros. O `.env` é ignorado pelo Git.

`APP_PUBLIC_BASE_URL` precisa ser uma URL HTTPS pública sintaticamente válida por causa das proteções de startup. Até a fase de domínio/HTTPS, links transacionais gerados com essa URL não serão navegáveis no ambiente local. As credenciais SMTP também precisam estar completas; use uma conta de teste externa somente quando o envio real for desejado.

## 2. Build das imagens

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml build api web migrate
```

## 3. Primeiro boot e migrations controladas

Inicie apenas o banco:

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml up -d postgres
```

Execute as migrations uma única vez, de forma explícita:

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml --profile tools run --rm migrate
```

O startup normal da API nunca executa migrations. Depois da atualização controlada, suba API e Web:

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml up -d api web
```

A aplicação fica disponível em `http://localhost:8080`. A ausência de HTTPS é deliberada nesta etapa local e não representa a topologia final de produção.

## 4. Status e health

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml ps
curl.exe --fail http://localhost:8080/health/ready
docker compose --env-file .env -f docker/docker-compose.production.yml exec api curl --fail http://localhost:8080/health/ready
```

O readiness da API inclui PostgreSQL. As respostas dos endpoints permanecem mínimas.

## 5. Logs

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml logs --follow api web
```

API e Web escrevem JSON em stdout/stderr. Não existem arquivos de log persistidos pelos containers.

## 6. Parar sem apagar dados

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml down
```

## 7. Recriar containers preservando volumes

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml up -d --force-recreate
```

Os volumes de PostgreSQL, Data Protection e logos permanecem. Sessões BFF estão em memória: reiniciar o Web encerra sessões atuais, embora as chaves de cookie permaneçam persistidas. Essa limitação é aceita enquanto houver apenas uma réplica.

## 8. Destruir o ambiente

> **DESTRUTIVO:** o comando abaixo remove também banco, chaves e logos. Use somente quando a perda integral do ambiente for intencional.

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml down --volumes
```
