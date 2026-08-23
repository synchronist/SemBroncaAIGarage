<div align="center">

# 🔧 SemBroncaAI Garage

### Gestão de oficinas sem burocracia, do atendimento à entrega.

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://docs.docker.com/compose/)
[![Tests](https://img.shields.io/badge/tests-xUnit-22A699)](https://xunit.net/)
[![Status](https://img.shields.io/badge/status-MVP-orange)](#-status-do-produto)

**Você conserta carros. Nós cuidamos da operação.**

[🌐 **Acessar demonstração online**](https://sembronca-garage.onrender.com/)

</div>

> O plano gratuito pode levar alguns segundos para despertar após um período sem acessos.

---

## 🧪 Demonstração online

| | Acesso |
|---|---|
| 🌐 Aplicação | [sembronca-garage.onrender.com](https://sembronca-garage.onrender.com/) |
| 👤 Usuário | `demo.owner` |
| 🔑 Senha | Solicite ao mantenedor — credenciais não são versionadas |
| 🏢 Ambiente | Oficina demo compartilhada, plano Standard em Trial |

Explore o fluxo completo: clientes, veículos, ordens de serviço, diagnóstico, orçamento e operação da oficina.

> Use somente dados fictícios. O conteúdo é compartilhado entre visitantes e pode ser restaurado periodicamente. O acesso PlatformAdmin não é público.

## ✨ O produto

O **SemBroncaAI Garage** é uma plataforma SaaS multi-oficina que centraliza clientes, veículos e ordens de serviço em um fluxo simples, seguro e rastreável.

| 🧰 Operação | 💬 Cliente | 🏢 Gestão |
|---|---|---|
| Clientes e veículos | Orçamentos digitais | PlatformAdmin multi-tenant |
| Diagnóstico e execução | Aprovação por link seguro | Equipe, roles e convites |
| Peças, serviços e PDF | Compartilhamento via WhatsApp | Trial, assinatura e auditoria |

## 🧭 Fluxo principal

```text
Recebido → Diagnóstico → Aprovação → Em execução → Finalizado → Entregue
```

Cada oficina opera em seu próprio tenant, identificado por `GarageId`, com isolamento aplicado na API e na persistência.

## 🧱 Arquitetura

```text
Blazor Web (BFF) → API → Application → Domain
                              ↑
                      Infrastructure
```

| Projeto | Responsabilidade |
|---|---|
| `Domain` | Entidades, invariantes e fluxo da oficina |
| `Application` | Casos de uso, contratos e permissões |
| `Infrastructure` | EF Core, Identity, PostgreSQL e serviços |
| `Api` | Endpoints, autenticação e health checks |
| `Web` | Blazor Server, MudBlazor e experiência do usuário |

## ⚙️ Stack

**.NET 10** · **ASP.NET Core** · **Blazor** · **MudBlazor** · **EF Core** · **PostgreSQL 17** · **Docker Compose** · **xUnit**

## 🚀 Começando

### Desenvolvimento

Pré-requisitos: **.NET 10 SDK** e **PostgreSQL 17**.

```powershell
dotnet restore
dotnet build
dotnet test
```

Credenciais locais devem ser configuradas com `dotnet user-secrets`; nunca em `appsettings` ou arquivos versionados.

### Production-like local

Pré-requisitos: **Docker Desktop** e um `.env` local criado a partir de `.env.example`.

```powershell
docker compose --env-file .env -f docker/docker-compose.production.yml build
docker compose --env-file .env -f docker/docker-compose.production.yml up -d postgres
docker compose --env-file .env -f docker/docker-compose.production.yml --profile tools run --rm migrate
docker compose --env-file .env -f docker/docker-compose.production.yml up -d api web
```

Aplicação: `http://localhost:8080` · Health: `/health/live` e `/health/ready`

> O procedimento completo, incluindo migrations, bootstrap e deploy seguro, está no [guia de produção local](docs/deployment-local-production.md).

## 🔐 Segurança por padrão

- ASP.NET Core Identity com lockout e password policy;
- Blazor como BFF: bearer permanece no servidor e o navegador recebe cookie `HttpOnly`;
- autorização por role, permissão e tenant;
- antiforgery, rate limiting e security stamp;
- convites e aprovações com expiração, uso único e hash persistido;
- Data Protection e assets em volumes persistentes;
- secrets, tokens e `.env` fora do Git.

## 🗂️ Estrutura

```text
src/      # Domain, Application, Infrastructure, API e Web
tests/    # Testes automatizados
docker/   # Stack production-like
scripts/  # Backup, restore, deploy e rollback
docs/     # Guias operacionais e decisões futuras
```

## 📚 Operação

| Guia | Quando usar |
|---|---|
| [Produção local](docs/deployment-local-production.md) | Primeiro boot e validação via Docker |
| [Deploy e rollback](docs/deployment-runbook.md) | Atualização segura da aplicação |
| [Backup e restore](docs/backup-restore.md) | Proteção e recuperação do PostgreSQL |
| [Bootstrap PlatformAdmin](docs/platform-admin-bootstrap.md) | Primeiro acesso administrativo |

## 📍 Status do produto

MVP em evolução ativa. Voz/IA, billing automatizado, gateway e WhatsApp transacional permanecem fora do escopo atual até implementação explícita.

---

<div align="center">

Feito para oficinas que querem trabalhar com organização e **sem bronca**. 🧡

</div>
