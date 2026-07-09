# rendatop
Sistema de investimentos

# Getting Stated

Create an `.env` file and:
```bash
cd server && ln ../.env .env && \
cd ../client && ln ../.env .env && \
cd ../landing/next-version && ln ../../.env .env
```

## Tracking de visitas da landing

O projeto registra a origem de visitas da landing page usando o parâmetro `visit` na URL.

Exemplos:
```text
http://localhost:3000/landing?visit=instagram
http://localhost:3000/landing?visit=youtube
http://localhost:5173/?visit=google
```

Quando a landing é aberta, o frontend envia um `POST` para:
```text
POST /public/landing-visits
```

O backend salva:
- `visit`
- `ip_address`
- `user_agent`
- `referrer`
- `created_at`

Se a URL não tiver `visit`, o valor salvo será `direct`.

### Frontends que registram a visita

- App React/Vite em [client/src/pages/Login.jsx](/home/hudsonventura/sources/rendatop/client/src/pages/Login.jsx)
- Landing Next.js em [landing-page-content.tsx](/home/hudsonventura/sources/rendatop/landing/nextjs-version/src/app/landing/landing-page-content.tsx)

### Configuração de ambiente

Para desenvolvimento local, mantenha no `.env`:
```bash
BASE_URL_SERVER=http://localhost:5000
BASE_URL_CLIENT=http://localhost:5173
BASE_URL_LANDING=http://localhost:3000
CORS_ORIGINS=${BASE_URL_CLIENT},${BASE_URL_LANDING}
```

Importante:
- `BASE_URL_LANDING` deve incluir a porta correta, por exemplo `http://localhost:3000`
- o backend expande os placeholders de `CORS_ORIGINS` automaticamente
- depois de alterar o `.env`, reinicie o backend

Para a landing Next.js, defina também:
```bash
NEXT_PUBLIC_API_URL=http://localhost:5000
```

Se quiser validar rapidamente, abra a landing com `?visit=instagram` e confirme no backend se a tabela `landing_visits` recebeu o registro.

### Integrações sociais do blog

Para habilitar a publicação automática do blog nas redes sociais, configure também:

```bash
META_GRAPH_API_VERSION=v23.0
FACEBOOK_PAGE_ID=
FACEBOOK_PAGE_ACCESS_TOKEN=
FACEBOOK_LONG_LIVED_USER_ACCESS_TOKEN=
INSTAGRAM_BUSINESS_ACCOUNT_ID=
INSTAGRAM_ACCESS_TOKEN=
LINKEDIN_ORGANIZATION_ID=
LINKEDIN_ACCESS_TOKEN=
LINKEDIN_API_VERSION=202504
```

Observações:
- Facebook usa `FACEBOOK_PAGE_ID` e `FACEBOOK_PAGE_ACCESS_TOKEN`
- se `FACEBOOK_PAGE_ACCESS_TOKEN` falhar, o backend também tenta `FACEBOOK_LONG_LIVED_USER_ACCESS_TOKEN` e depois `FACEBOOK_USER_ACCESS_TOKEN`, nessa ordem
- imagens enviadas às redes sociais são expostas por URLs temporárias do backend em vez de `data:` URL/base64
- Instagram exige uma conta business e pelo menos uma imagem no post
- LinkedIn publica em nome da organização definida em `LINKEDIN_ORGANIZATION_ID`
- se uma rede não estiver configurada, o post continua sendo publicado no blog e o canal fica com status de falha no painel admin

### Renovação do token da página do Facebook

Para reemitir o `long-lived user token` e listar os `page access tokens` disponíveis:

```bash
export FACEBOOK_APP_ID=*** && \
export FACEBOOK_APP_SECRET=*** && \
export FACEBOOK_USER_ACCESS_TOKEN='***' && \
bash scripts/facebook-refresh-page-token.sh
```

O script:
- troca o `FACEBOOK_USER_ACCESS_TOKEN` por um `long-lived user token`
- chama `GET /me/accounts`
- imprime `FACEBOOK_PAGE_ID` e `FACEBOOK_PAGE_ACCESS_TOKEN` prontos para copiar no `.env`

Observações:
- o `FACEBOOK_USER_ACCESS_TOKEN` informado ao script deve ser um token de usuário válido
- o `FACEBOOK_PAGE_ACCESS_TOKEN` retornado é o token que o backend usa para publicar na página
- se o usuário perder acesso à página ou revogar permissões, será necessário rodar o fluxo novamente


### Migrations
``` bash
dotnet ef migrations add NameMigration --project server/server.csproj
dotnet ef database update --project server/server.csproj
```

## dbclone

O repositório possui um console app em `.NET 10` para clonar um banco PostgreSQL de origem para um banco de destino, recriando estrutura e dados completos.

Documentacao detalhada:

- [dbclone/README.md](/home/hudsonventura/sources/rendatop/dbclone/README.md)

Pre-requisitos no host:

- o utilitario usa `pg_dump` e `pg_restore`
- nao basta o banco estar no Docker; os binarios precisam estar instalados na maquina

No Ubuntu:

```bash
sudo apt update
sudo apt install postgresql-client
```

Ou, para acompanhar a versao principal `18` usada no ambiente dev:

```bash
sudo apt update
sudo apt install postgresql-client-18
```

Teste rapido:

```bash
pg_dump --version
pg_restore --version
```

Execucao:

```bash
dotnet run --project dbclone/dbclone.csproj
```

# Authentications Configuration

**Microsoft**
https://entra.microsoft.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/21b69e98-264e-4bc8-8e1e-ed8405355fc4/isMSAApp~/false


**Google**
https://console.cloud.google.com/auth/branding?authuser=1&project=rendatop






## Evolution
http://10.10.1.202:8080/Manager
Senha no .env -> AUTHENTICATION_API_KEY

## WhatsApp via WWebJS API
O projeto agora pode usar o `wwebjs-api` como provider principal, mantendo a Evolution como fallback.

Use as variáveis de `.env.add`:

```bash
WHATSAPP_PROVIDER=wwebjs
WHATSAPP_PROVIDER_FALLBACK=evolution
WHATSAPP_WWEBJS_URL=http://whatsapp-wwebjs:3000
WHATSAPP_WWEBJS_API_KEY=CHANGE_ME_WWEBJS
WHATSAPP_WWEBJS_SESSION_ID=Default
```

O container recomendado é `avoylenko/wwebjs-api:v1.34.6`, com sessão persistida em volume local.

```bash
export $(xargs < .env) && \
curl -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" $WHATSAPP_WWEBJS_URL/session/start/$WHATSAPP_WWEBJS_SESSION_ID
```

Para solicitar o pairing code:

```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" \
  -d '{"phoneNumber":"$PHONE","showNotification":true}' \
  $WHATSAPP_WWEBJS_URL/session/requestPairingCode/$WHATSAPP_WWEBJS_SESSION_ID
```

Para solicitar o QR Code:

```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" \
  $WHATSAPP_WWEBJS_URL/session/qr/$WHATSAPP_WWEBJS_SESSION_ID/image \
  --output qrcode.png
```

O envio de mensagens usa o endpoint `POST /client/sendMessage/:sessionId` do próprio `wwebjs-api`.

## Mercado Pago
### Documentação

### Create app and Generate Tokens
https://www.mercadopago.com.br/developers/panel/app/8241518968298990

### Variáveis de ambiente
Para o checkout hospedado com assinaturas, configure no `.env` raiz:

```bash
BASE_URL_SERVER=https://api.seu-dominio.com
BASE_URL_CLIENT=https://app.seu-dominio.com
MERCADO_PAGO_WEBHOOK_CALLBACK=https://app.seu-dominio.com/subscription/mercado-pago/return
MERCADO_PAGO_ACCESS_TOKEN=APP_USR-...
MERCADO_PAGO_WEBHOOK_SECRET=...
MERCADO_PAGO_WEBHOOK_URL=https://api.seu-dominio.com/subscription/webhook/mercado-pago
MERCADO_PAGO_STATEMENT_DESCRIPTOR=RENDATOP
```

Observações:
- `BASE_URL_SERVER` precisa ser a URL pública HTTPS do backend, porque o Mercado Pago chamará `POST {BASE_URL_SERVER}/subscription/webhook/mercado-pago`
- `MERCADO_PAGO_WEBHOOK_CALLBACK` precisa ser a URL pública HTTPS completa de retorno do usuário, por exemplo `https://app.seu-dominio.com/subscription/mercado-pago/return`
- `MERCADO_PAGO_WEBHOOK_URL`, quando informada, passa a ser usada explicitamente pelo backend como `notification_url`
- `MERCADO_PAGO_WEBHOOK_URL` não deve ser usada como `back_url` do navegador; o retorno do usuário sempre deve apontar para o frontend
- `localhost` não serve para `back_url` nem para webhook em ambiente real do Mercado Pago
- se estiver testando localmente, exponha frontend e backend com uma URL pública temporária, como `ngrok` ou `Cloudflare Tunnel`
- a aplicação lê essas variáveis do `.env` raiz; se `server/.env` for um link/junction para esse arquivo, continua funcionando

### Configuração no painel do Mercado Pago
Em `Suas integrações`:

1. Gere ou copie o `Access Token` da aplicação e preencha `MERCADO_PAGO_ACCESS_TOKEN`.
2. Abra `Webhooks` da mesma aplicação.
3. Cadastre a URL pública do backend:
   `https://rendatop.com.br/api/subscription/webhook/mercado-pago`
4. Ative pelo menos estes tópicos:
   - `payment`
   - `subscription_preapproval`
   - `subscription_authorized_payment`
5. Revele a assinatura secreta gerada pelo Mercado Pago e preencha `MERCADO_PAGO_WEBHOOK_SECRET`.

Importante:
- o sistema cria o link de assinatura dinamicamente a cada contratação; não é preciso pré-criar links por plano
- o retorno do navegador para `/subscription/mercado-pago/return` é apenas UX; a ativação final depende do webhook
- cancelamentos efetivos interrompem a recorrência no Mercado Pago, e estornos continuam sendo feitos sobre o `payment_id` original

### Troubleshooting do Checkout Pro
Se o backend retornar `Falha ao comunicar com o Mercado Pago ao criar o checkout hospedado. An unexpected error has occurred.`, verifique:

1. `MERCADO_PAGO_WEBHOOK_CALLBACK` aponta para a URL pública completa de retorno do usuário, e não para a URL do webhook/backend.
2. `MERCADO_PAGO_WEBHOOK_URL` aponta para o backend público, por exemplo:
   `https://api.seu-dominio.com/api/subscription/webhook/mercado-pago`
3. O retorno do navegador deve chegar em:
   `https://app.seu-dominio.com/subscription/mercado-pago/return`
   e não em `/subscription/webhook/mercado-pago`
4. O `MERCADO_PAGO_ACCESS_TOKEN` pertence à mesma aplicação/conta usada para configurar webhook e Checkout Pro.
5. O ambiente do token (`TEST-...` ou produção) é o mesmo ambiente da conta/aplicação que você está testando.
6. Na conta/aplicação do Mercado Pago, os meios que você quer usar no Checkout Pro estão disponíveis para essa conta. Se Pix ou boleto não estiverem habilitados na conta, a preference pode falhar ou o checkout pode não exibir o meio esperado.

### Exemplo para desenvolvimento local com túnel
Se o frontend local roda em `http://localhost:5173` e o backend local em `http://localhost:5000`, não envie essas URLs ao Mercado Pago.

Use algo assim no `.env`:

```bash
BASE_URL_SERVER=https://api-teste.seu-tunel.dev
BASE_URL_CLIENT=https://app-teste.seu-tunel.dev
MERCADO_PAGO_WEBHOOK_CALLBACK=https://app-teste.seu-tunel.dev/subscription/mercado-pago/return
MERCADO_PAGO_WEBHOOK_URL=https://api-teste.seu-tunel.dev/subscription/webhook/mercado-pago
```

Depois:
- o navegador do usuário voltará para `https://app-teste.seu-tunel.dev/subscription/mercado-pago/return`
- o webhook do Mercado Pago chamará `https://api-teste.seu-tunel.dev/subscription/webhook/mercado-pago`

### Statement descriptor da fatura
Configure a variável de ambiente `MERCADO_PAGO_STATEMENT_DESCRIPTOR` com o nome que deve aparecer na fatura do cartão.

Exemplo:
```bash
MERCADO_PAGO_STATEMENT_DESCRIPTOR=RENDATOP
```

Observações:
- o backend envia esse valor ao Mercado Pago quando a cobrança direta por cartão ainda for usada internamente
- o texto é normalizado para remover acentos e caracteres especiais
- o valor é truncado para no máximo 22 caracteres


## Tests
```bash
sudo docker compose -f docker-compose-tests.yml down --remove-orphans && \
sudo docker compose -f docker-compose-tests.yml up && \
sudo docker compose -f docker-compose-tests.yml down --remove-orphans
```

## Build release do app mobile

O app mobile fica em `mobile/RendaTop.App` e, em `Release`, usa a URL de producao definida em [AppConfig.cs](/home/hudsonventura/source/rendatop/mobile/RendaTop.App/Services/AppConfig.cs).

Build release simples:

```bash
dotnet build mobile/RendaTop.App/RendaTop.App.csproj -c Release
```

Criar um keystore para assinatura:

```bash
keytool -genkeypair \
  -v \
  -storetype PKCS12 \
  -keystore rendatop-release.keystore \
  -alias rendatop \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

Gerar `AAB` assinado para Google Play:

```bash
export SENHA=SUA_SENHA

./scripts/publish-mobile-release.sh
```

Gerar `APK` assinado para instalacao manual:

```bash
dotnet publish mobile/RendaTop.App/RendaTop.App.csproj \
  -f net10.0-android \
  -c Release \
  -p:AndroidPackageFormat=apk \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=/caminho/para/rendatop-release.keystore \
  -p:AndroidSigningStorePass=SUA_SENHA \
  -p:AndroidSigningKeyAlias=rendatop \
  -p:AndroidSigningKeyPass=SUA_SENHA
```

Saida esperada:
- build: `mobile/RendaTop.App/bin/Release/net10.0-android/`
- publish: `mobile/RendaTop.App/bin/Release/net10.0-android/publish/`

Observacoes:
- quando voce compila em `Release`, o app aponta para `https://api.rendatop.com.br`
- quando voce depura pelo VS Code com debugger anexado, o app usa o endpoint local definido no `AppConfig`
- para Google Play, prefira enviar o arquivo `.aab`
- guarde com cuidado o `keystore`, o `alias` e as senhas
- nao versionar senhas de assinatura no repositorio


# Dados do Dominio Production e Testing
- Dominio base: `rendatop.com.br`
- URL PROD: `rendatop.com.br` ou `www.rendatop.com.br`
- URL Testing: `testing-landing.rendatop.com.br`
- Local do registro: https://registro.br/login/?session=required
- Nome de usuário: `HUVEN23`
- Válido até: **31/05/2028**


# Logos
https://commons.wikimedia.org/wiki/Category:SVG_logos_of_banks_in_Brazil
https://github.com/Tgentil/Bancos-em-SVG/
