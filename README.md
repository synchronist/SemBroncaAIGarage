# SemBroncaAI Garage

Você conserta carros. Nós cuidamos da burocracia.

## Sobre o produto

O SemBroncaAI Garage é uma plataforma para oficinas mecânicas que busca reduzir o tempo gasto com atendimento, organização, documentação e relacionamento com clientes.

## Objetivo do MVP

O primeiro MVP permitirá:

- cadastrar oficinas;
- cadastrar clientes;
- cadastrar veículos;
- registrar atendimentos;
- criar ordens de serviço;
- adicionar peças e serviços;
- calcular valores;
- gerar documentos;
- preparar mensagens para envio ao cliente.

## Tecnologias

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Docker
- xUnit

## Estrutura

```text
src/
  SemBroncaAI.Garage.Api
  SemBroncaAI.Garage.Application
  SemBroncaAI.Garage.Domain
  SemBroncaAI.Garage.Infrastructure
  SemBroncaAI.Garage.SharedKernel

tests/
  SemBroncaAI.Garage.Tests

docs/
docker/
```

## Identidade visual e armazenamento local

A oficina armazena no PostgreSQL somente `LogoStorageKey` e `PrimaryColor`. Em desenvolvimento, os arquivos são gravados pelo storage local configurado em `BrandAssets:RootPath` (padrão: `brand-assets` ao lado da aplicação publicada). A pasta é ignorada pelo Git. A abstração `IBrandAssetStorage` permite substituir essa implementação por object storage futuramente.

Logos aceitas: PNG, JPEG ou WebP, com até 2 MB. O servidor valida MIME e assinatura binária e gera a chave do arquivo; o nome enviado pelo usuário não é usado no caminho.

## Geração de PDF com Playwright

Os PDFs são produzidos pela API renderizando as rotas HTML imprimíveis do Web com Chromium. Configure `Web:BaseUrl` com a URL acessível do projeto Web.

Após restaurar/compilar, instale o Chromium compatível com o pacote:

```powershell
pwsh src/SemBroncaAI.Garage.Api/bin/Release/net10.0/playwright.ps1 install chromium
```

No Linux/Docker, instale também as dependências nativas:

```bash
pwsh src/SemBroncaAI.Garage.Api/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
```

## Aprovação digital do orçamento

Os links públicos expiram em sete dias. O token possui 256 bits aleatórios; somente seu hash SHA-256 e uma cópia protegida pelo ASP.NET Core Data Protection são persistidos. Em containers, preserve o key ring do Data Protection entre deploys para que links ativos continuem recuperáveis pela oficina após reinicializações.

Os endpoints públicos possuem rate limit por IP. A página do cliente fica em `/approval/{token}` e não exige nem expõe `GarageId`.

Antes de publicar em Docker, monte o diretório do key ring do ASP.NET Core Data Protection em volume persistente compartilhado entre réplicas, ou use um provedor externo persistente. Proteja também essas chaves em repouso.

Como o token faz parte da URL, configure reverse proxy, access logs, APM e tracing para não registrar paths completos de `/approval/{token}` e `/api/public/approvals/{token}`. Atrás de proxy ou ingress, configure e restrinja `ForwardedHeaders` aos proxies conhecidos para que o rate limit utilize o IP real do cliente sem confiar em cabeçalhos forjados.

Em produção, fixe a versão do pacote e do browser. A API e o Web precisam estar acessíveis entre si; em containers, `Web:BaseUrl` deve usar o hostname interno do serviço, não `localhost`.
