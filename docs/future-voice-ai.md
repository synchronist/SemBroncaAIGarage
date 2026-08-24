# Futuro: voz e IA no recebimento

O ponto natural para a futura experiência é o campo **Relato do cliente** da tela **Receber veículo** (`Home.razor`). A ação removida se chamava **Ditar relato** e não possuía implementação.

Quando essa capacidade for priorizada, o fluxo deverá ser explícito e revisável:

1. solicitar consentimento e acesso ao microfone;
2. capturar áudio com indicação visual clara;
3. transcrever por um serviço definido;
4. opcionalmente estruturar o relato com IA sem inventar informações;
5. preencher o campo de relato;
6. exigir revisão e confirmação do usuário antes de criar a ordem de serviço.

A definição futura também deverá cobrir privacidade, retenção ou descarte do áudio, falhas de permissão, navegadores suportados, custos e observabilidade. Nenhuma captura, Web Speech API, transcrição, storage ou integração de IA foi adicionada nesta fase.
