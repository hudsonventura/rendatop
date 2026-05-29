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

Para iniciar a sessão manualmente:

```bash
WHATSAPP_WWEBJS_API_KEY=sua_key
WHATSAPP_WWEBJS_API_URL=http://10.10.1.202:3000
SESSION=RendaTop
PHONE=11912466241
```

```bash
curl -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" $WHATSAPP_WWEBJS_API_URL/session/start/$SESSION
```

Para solicitar o pairing code:

```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" \
  -d '{"phoneNumber":"$PHONE","showNotification":true}' \
  $WHATSAPP_WWEBJS_API_URL/session/requestPairingCode/$SESSION
```

Para solicitar o QR Code:

```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" \
  $WHATSAPP_WWEBJS_API_URL/session/qr/$SESSION/image \
  --output qrcode.png
```

O envio de mensagens usa o endpoint `POST /client/sendMessage/:sessionId` do próprio `wwebjs-api`.

## Mercado Pago
### Documentação

### Create app and Generate Tokens
https://www.mercadopago.com.br/developers/panel/app/8241518968298990

### Statement descriptor da fatura
Configure a variável de ambiente `MERCADO_PAGO_STATEMENT_DESCRIPTOR` com o nome que deve aparecer na fatura do cartão.

Exemplo:
```bash
MERCADO_PAGO_STATEMENT_DESCRIPTOR=RENDATOP
```

Observações:
- o backend envia esse valor no campo `statement_descriptor` ao criar pagamentos com cartão
- o texto é normalizado para remover acentos e caracteres especiais
- o valor é truncado para no máximo 22 caracteres


## Tests
```bash
sudo docker compose -f docker-compose-tests.yml down --remove-orphans && \
sudo docker compose -f docker-compose-tests.yml up && \
sudo docker compose -f docker-compose-tests.yml down --remove-orphans
```
