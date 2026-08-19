# SemBroncaAI Garage

> A política de retenção das entradas de auditoria administrativa será definida antes da produção comercial definitiva. Nesta fase não há limpeza automática.

Instruções do ambiente production-like: [Produção local com Docker Compose](docs/deployment-local-production.md).

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

## Compartilhamento manual pelo WhatsApp

A Central de Orçamentos apenas prepara a mensagem e abre `wa.me`; o envio continua sendo confirmado manualmente pela oficina e não é registrado como enviado. Configure `PublicAppBaseUrl` no projeto Web com a origem pública absoluta da aplicação (HTTPS em produção), por exemplo `https://garage.exemplo.com/`. Essa URL é usada para compor links de aprovação sem depender de `localhost` ou do endereço interno do container.

Em desenvolvimento local, deixe `PublicAppBaseUrl` vazio em `src/SemBroncaAI.Garage.Web/appsettings.Development.json`. Nesse caso, a Central usa a origem real pela qual o navegador abriu o Web (`NavigationManager.BaseUri`), funcionando tanto com o perfil HTTP quanto com o HTTPS sem presumir uma porta. Para testar com IP, túnel ou outro endereço público, preencha `PublicAppBaseUrl` explicitamente; o valor configurado tem precedência sobre a origem do navegador.

`localhost` funciona somente no próprio computador. Para abrir o link em um celular, configure `PublicAppBaseUrl` com o IP da máquina acessível na rede local, um túnel HTTPS ou o domínio público da aplicação. O Web também precisa estar escutando nesse endereço e qualquer certificado/firewall deve permitir o acesso; esta aplicação não cria nem gerencia túneis automaticamente.

## Identity em Development

O seed de Identity roda somente no ambiente `Development`, usa a Garage indicada em `IdentitySeed:GarageId` e é idempotente para as roles `PlatformAdmin`, `Owner`, `Receptionist` e `Mechanic` e para o Owner local. Ele nunca cria uma Garage e não aplica migrations automaticamente. Antes de iniciar a API após aplicar manualmente a migration de Identity, configure a senha fora do repositório:

```powershell
dotnet user-secrets set "IdentitySeed:OwnerPassword" "sua-senha-local" --project src/SemBroncaAI.Garage.Api
dotnet user-secrets set "IdentitySeed:ReceptionistPassword" "sua-senha-local" --project src/SemBroncaAI.Garage.Api
dotnet user-secrets set "IdentitySeed:MechanicPassword" "sua-senha-local" --project src/SemBroncaAI.Garage.Api
dotnet user-secrets set "IdentitySeed:PlatformAdminPassword" "sua-senha-local" --project src/SemBroncaAI.Garage.Api
```

Alternativamente, defina a variável `IdentitySeed__OwnerPassword`. Sem senha, com Garage inexistente ou com usuário associado a outra Garage, a inicialização em Development falha com mensagem explícita. A senha deve ter ao menos 10 caracteres, letras maiúsculas e minúsculas e um dígito; símbolo não é obrigatório. O lockout está preparado para 15 minutos após 5 tentativas inválidas.

## Autenticação Web e API

O Blazor Interactive Server funciona como BFF. O navegador recebe apenas o cookie de sessão do Web (`HttpOnly`, `SameSite=Lax`; `Secure` e prefixo `__Host-` em Production). O login é enviado por POST com antiforgery ao Web, que valida as credenciais na API usando ASP.NET Core Identity. A API emite seu bearer opaco oficial, e o Web o guarda somente no servidor, associado a um identificador aleatório presente no cookie. O token não é enviado ao JavaScript nem armazenado em `localStorage` ou `sessionStorage`.

Sessões marcadas com **Lembrar meu acesso** usam cookie persistente; as demais usam cookie de sessão. Cookie e bearer têm limite máximo de sete dias, e o cookie possui sliding expiration dentro desse limite. A API revalida `Active`, vínculo com Garage e security stamp em `/api/auth/me`. Cinco senhas inválidas bloqueiam a conta por 15 minutos; os POSTs de login do Web e da API também possuem limite por IP de 10 tentativas por minuto.

O cofre de credenciais do BFF usa memória local nesta fase incremental. Reiniciar o Web invalida as sessões existentes, e múltiplas réplicas exigirão um store distribuído protegido antes de produção. Não registre cookies, cabeçalhos `Authorization`, senhas nem corpos do login em access logs/APM. O logout remove a credencial server-side e o cookie. Antiforgery protege login/logout baseados em cookie; a chamada Web→API usa bearer e não depende de cookie do navegador.

O endpoint autenticado da API é `GET /api/auth/me`. Para validar a cadeia completa no navegador, `GET /auth/me` no Web usa o cookie e consulta esse endpoint da API server-side; o bearer nunca chega ao browser. Os endpoints `/api/public/approvals/{token}` e `/approval/{token}` continuam anônimos.

## Recuperação de senha

A recuperação usa os token providers oficiais do ASP.NET Core Identity. O token expira em duas horas; uma redefinição bem-sucedida atualiza o security stamp. Em Development sem SMTP, o link é escrito somente no log. Em Production, configure o transporte SMTP e `PasswordRecovery:Enabled=true`; falhas de entrega preservam a resposta pública neutra.

## E-mail transacional

Convites e recuperação usam um transporte SMTP genérico, com HTML e texto puro. Configure por secret ou variável de ambiente: `Email__Provider=Smtp`, `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__FromAddress`, `Email__FromName`, `Email__UseSsl` e `Email__TimeoutSeconds`. Configure também `App__PublicBaseUrl` com a origem HTTPS pública usada nos links. Nenhuma credencial deve ser versionada.

## Configuração de Production

Os arquivos base não contêm connection string nem URLs locais como fallback de Production. Forneça configurações por variáveis de ambiente, secret store ou arquivo não versionado. API e Web falham na inicialização quando faltar configuração crítica:

- API: `ConnectionStrings__DefaultConnection`, `Web__BaseUrl`, `App__PublicBaseUrl`, configurações `Email__*`, `DataProtection__KeysPath` e `ReverseProxy__KnownProxies__0`;
- Web: `Api__BaseUrl`, `DataProtection__KeysPath` e `ReverseProxy__KnownProxies__0`;
- `IdentitySeed__Enabled` e `PasswordRecovery__Enabled` devem permanecer `false` em Production nesta fase.

`DataProtection__KeysPath` deve apontar para storage persistente, compartilhado por todas as réplicas da mesma aplicação e protegido em repouso. API e Web usam nomes estáveis e distintos (`SBGarage.Api` e `SBGarage.Web`). Sem persistência, links protegidos da API e cookies do Web podem ser invalidados após redeploy.

Configure somente os IPs reais dos proxies diretamente confiáveis em `ReverseProxy__KnownProxies__N`. A aplicação processa um salto de `X-Forwarded-For`/`X-Forwarded-Proto`, antes de HTTPS, autenticação e rate limiting. Não aceite esses headers diretamente da internet.

O storage atual de logos é filesystem local em `BrandAssets:RootPath`; somente a chave fica no PostgreSQL. Em host efêmero, monte esse diretório em volume persistente ou a logo será perdida no redeploy.

O cofre de sessões do BFF ainda é memória por processo. Restart encerra sessões; logout remove a entrada e entradas expiradas são removidas quando consultadas ou quando uma nova sessão é criada. Isso é aceito para o primeiro piloto em uma única VPS. Múltiplas réplicas exigem afinidade como medida temporária e, antes de escalar com confiabilidade, um store distribuído protegido. Persistir Data Protection não distribui o bearer guardado no BFF.

O bearer da API possui validade máxima de sete dias e não é revogado no logout nesta fase. Ele fica somente no cofre server-side do Web, não no navegador; após logout a referência da sessão é removida. Revogação real será necessária se tokens passarem a clientes externos, houver suspeita de extração do processo, exigência de encerramento imediato em todas as réplicas ou sessões distribuídas.

## Isolamento de tenant

Os módulos internos obtêm o tenant exclusivamente da claim `garage_id` emitida pelo Identity. `ICurrentUser` e `ICurrentGarage` expõem esse contexto sem dependência de `HttpContext`; a API valida usuário ativo, security stamp e Garage existente. Customers, Vehicles, Lookup, Service Orders, Estimates, Settings, branding e documentos não aceitam mais `GarageId` em query string, rota ou body. Os repositories continuam filtrando por Garage como defesa em profundidade.

A API exige autenticação por fallback policy. Login e aprovação pública são os únicos endpoints marcados com `AllowAnonymous`. Operações tenant exigem a policy `TenantUser`; `PlatformAdmin` sem Garage não recebe acesso operacional. A configuração `Garage:Id` permanece somente no seed da API em Development e não é usada pelo Web para autorização.

Logo e PDF também preservam o tenant autenticado. A logo interna passa por endpoint protegido do BFF. Para PDF, a API encaminha ao Playwright o bearer opaco já autenticado somente como header server-to-server; a rota imprimível valida a credencial contra a API e não a expõe em HTML ou JavaScript.
