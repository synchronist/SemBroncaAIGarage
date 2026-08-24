# Futuro: planos, cobrança e gateway

A tela **Plano e assinatura** apresenta apenas a assinatura atual da oficina e condições comerciais informativas. Nesta fase não existe contratação, cobrança, renovação automática nem integração com gateway.

Quando o produto evoluir, o fluxo poderá incluir catálogo de planos, escolha de periodicidade, checkout hospedado por um provedor de pagamentos, confirmação assíncrona por webhook, conciliação e histórico de faturas. Estado comercial e estado operacional da oficina devem continuar separados e as transições devem ser auditáveis.

Dados sensíveis de cartão não devem transitar nem ser persistidos pelo SemBroncaAI Garage. Número completo do cartão e CVV nunca devem ser armazenados. A aplicação deverá guardar somente identificadores/tokens do gateway e metadados não sensíveis estritamente necessários, seguindo as regras do provedor e os requisitos aplicáveis de segurança e privacidade.
