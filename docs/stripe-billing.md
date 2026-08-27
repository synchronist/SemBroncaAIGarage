# Stripe Billing

O SemBroncaAI Garage usa Stripe Checkout hospedado para contratar o plano Standard e o Portal do Cliente para gerenciar cobrança. O estado local é sincronizado exclusivamente por webhooks assinados e idempotentes.

## Configuração

Configure apenas por variáveis de ambiente ou secret manager:

```text
Stripe__Enabled=true
Stripe__SecretKey=...
Stripe__WebhookSecret=...
Stripe__MonthlyPriceId=price_...
Stripe__AnnualPriceId=price_...
```

Use chaves e Price IDs de Sandbox em ambientes de teste. Nunca misture objetos `test` e `live` nem versione `sk_` ou `whsec_`.

## Webhook

Endpoint público da API:

```text
POST /api/billing/stripe/webhook
```

Eventos necessários:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.paid`
- `invoice.payment_failed`

O endpoint é anônimo somente para permitir chamadas da Stripe; cada payload é autenticado pelo cabeçalho `Stripe-Signature`. Eventos já processados são ignorados pelo identificador único persistido no PostgreSQL.

## Segurança

- a Web envia somente o ciclo `Monthly` ou `Annual`;
- a API escolhe o Price ID configurado;
- o `GarageId` vem do usuário autenticado e também é gravado nos metadados da Stripe;
- nenhum dado de cartão é recebido ou armazenado pela aplicação;
- alterações de status e período chegam por webhook, não pelo redirect do navegador.
