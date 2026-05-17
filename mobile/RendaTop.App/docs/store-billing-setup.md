# Store Billing Setup

Este app mobile agora possui uma base para decidir o fluxo de pagamento por plataforma:

- `Android` -> `Google Play`
- `iOS` -> `App Store`
- outros ambientes -> checkout direto existente

Arquivos principais:

- `Services/AppPlatformService.cs`
- `Services/StoreBillingService.cs`
- `Pages/StoreSubscriptionCheckoutPage.xaml`

Importante: hoje o backend ainda **nao possui** endpoints para validar recibos da Google Play ou da Apple App Store.  
Sem isso, uma compra nativa nao consegue ativar o plano com seguranca no servidor.

## O que ja ficou pronto no app

- deteccao centralizada da plataforma
- estrategia de checkout por plataforma
- tela separada para pagamento nativo por loja
- documentacao para configuracao futura

## O que ainda precisa existir no backend

Para fechar o fluxo de assinatura por loja, o backend precisa receber o comprovante da compra e validar com o provedor.

### Endpoints recomendados

Criar algo nesta linha:

- `POST /subscription/store/google/validate`
- `POST /subscription/store/apple/validate`

Payloads sugeridos:

### Google Play

```json
{
  "plan_id": "plus",
  "product_id": "br.com.rendatop.plus.monthly",
  "purchase_token": "token-da-compra",
  "package_name": "br.com.rendatop.app"
}
```

### Apple

```json
{
  "plan_id": "plus",
  "product_id": "br.com.rendatop.plus.monthly",
  "transaction_id": "id-da-transacao",
  "original_transaction_id": "id-original",
  "app_account_token": "uuid-opcional-do-usuario"
}
```

### Responsabilidades do backend

1. Validar o recibo/token com Google ou Apple.
2. Confirmar que o produto corresponde ao plano correto do app.
3. Garantir que a compra pertence ao usuario autenticado.
4. Ativar ou renovar a assinatura localmente.
5. Guardar identificadores da compra para auditoria e reconciliacao.
6. Processar renovacoes, cancelamentos e expiracoes via notificacoes dos stores.

## Android / Google Play

## 1. Criar os produtos

No `Google Play Console`:

1. Acesse o app `RendaTop`.
2. Abra `Monetize > Products > Subscriptions`.
3. Crie uma assinatura para cada plano pago.

Sugestao de SKUs:

- `br.com.rendatop.plus.monthly`
- `br.com.rendatop.pro.monthly`

Se quiser, mantenha o `plan_id` atual do backend (`plus`, `pro`) e crie um mapeamento app/backend.

## 2. Configurar licenciamento e testers

1. Abra `Monetize > Monetization setup`.
2. Configure o perfil de pagamentos.
3. Em `License testing`, cadastre os emails de teste.
4. Publique ao menos uma versao em faixa interna, fechada ou aberta.

## 3. Integrar Billing no app

Hoje o app ja tem a abstracao pronta, mas ainda falta a biblioteca de billing.

Opcao sugerida:

- integrar `Plugin.InAppBilling` ou outra biblioteca MAUI compativel com Google Play Billing

Pontos de integracao esperados:

- consultar produtos por SKU
- iniciar compra
- receber `purchaseToken`
- enviar token ao backend para validacao

## 4. Notificacoes do Google Play

Para renovacoes e cancelamentos automaticos:

1. Configure `Real-time developer notifications`.
2. Aponte para um backend/worker seu.
3. Esse worker deve atualizar a assinatura no servidor.

## iOS / Apple

## 1. Criar os produtos

No `App Store Connect`:

1. Abra o app `RendaTop`.
2. Acesse `Monetization > Subscriptions`.
3. Crie os produtos de assinatura.

Sugestao de Product IDs:

- `br.com.rendatop.plus.monthly`
- `br.com.rendatop.pro.monthly`

## 2. Assinar acordos e capacidades

1. Confirme que os agreements de `Paid Applications` estao ativos.
2. No projeto do app, habilite a capability `In-App Purchase`.

## 3. Integrar StoreKit

Hoje o app ja sabe que `iOS` deve usar a `App Store`, mas ainda falta a implementacao real do billing.

Pontos de integracao esperados:

- carregar produtos da App Store
- iniciar compra de assinatura
- receber `transactionId`
- enviar comprovante ao backend para validacao

## 4. App Store Server Notifications

Para renovacoes, expiracoes e cancelamentos:

1. Configure `App Store Server Notifications`.
2. Aponte para seu backend.
3. Atualize o status da assinatura local com base nos eventos recebidos.

## Mapeamento recomendado de planos

Mesmo mantendo o backend com `plan_id = plus/pro`, vale adotar um mapeamento explicito:

```text
plus -> br.com.rendatop.plus.monthly
pro  -> br.com.rendatop.pro.monthly
```

## Proximo passo tecnico recomendado

1. Adicionar uma biblioteca MAUI de in-app billing.
2. Implementar `StoreBillingService` para:
   - listar produtos
   - iniciar compra
   - devolver token/transacao
3. Criar endpoints de validacao de recibo no backend.
4. Trocar o estado atual de `StoreSubscriptionCheckoutPage` de informativo para compra real.
