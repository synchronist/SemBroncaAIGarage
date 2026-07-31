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