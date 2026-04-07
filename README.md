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
