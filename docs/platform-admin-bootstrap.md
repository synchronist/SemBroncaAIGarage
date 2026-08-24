# Bootstrap inicial do PlatformAdmin

O primeiro PlatformAdmin de uma instalação nova é criado somente por execução explícita do serviço de ferramentas. Ele não roda com a API, não possui senha padrão e não cria um segundo administrador.

1. Defina temporariamente `BOOTSTRAP_ADMIN_NAME`, `BOOTSTRAP_ADMIN_EMAIL`, `BOOTSTRAP_ADMIN_USERNAME` e `BOOTSTRAP_ADMIN_PASSWORD` no `.env` local ignorado pelo Git.
2. Execute `docker compose --env-file .env -f docker/docker-compose.production.yml --profile tools run --rm bootstrap-admin`.
3. Remova a senha do `.env` após o sucesso e armazene-a no gerenciador de segredos operacional.

Em uma instalação já configurada, o comando termina sem alteração e informa `PlatformAdmin já configurado.`. A senha nunca é exibida. O seed de Development continua sendo o mecanismo exclusivo do ambiente Development.
