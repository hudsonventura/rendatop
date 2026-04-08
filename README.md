# rendatop
Sistema de investimentos

# Getting Stated

Create an `.env` file and:
```bash
cd server && ln ../.env .env && cd ../client && ln ../.env .env
```


### Migrations
``` bash
dotnet ef migrations add NameMigration --project server/server.csproj
dotnet ef database update --project server/server.csproj
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
```

```bash
curl -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" $WHATSAPP_WWEBJS_API_URL:3000/session/start/Default
```

Para solicitar o pairing code:

```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" \
  -d '{"phoneNumber":"5565999999999","showNotification":true}' \
  $WHATSAPP_WWEBJS_API_URL:3000/session/requestPairingCode/Default
```

Para solicitar o QR Code:

```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "x-api-key: $WHATSAPP_WWEBJS_API_KEY" \
  $WHATSAPP_WWEBJS_API_URL:3000/session/qr/Default/image \
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
