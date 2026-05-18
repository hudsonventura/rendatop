# SSO mobile com Google e Microsoft

Este documento cobre o login social do app mobile MAUI Android com:

- Google
- Microsoft Entra ID

O fluxo usa o backend do RendaTop como intermediario para criar a sessao real do app.

## Como o fluxo funciona

O app nao depende do cookie do navegador externo.

O caminho e este:

1. o app abre:
   - `GET /auth/google/login?client=mobile`
   - ou `GET /auth/microsoft/login?client=mobile`
2. o backend redireciona para o provedor SSO
3. o provedor volta para o callback HTTP do backend
4. o backend detecta `client=mobile`
5. o backend cria um `handoff_token` curto no Redis
6. o backend redireciona para o app:

```text
br.com.rendatop.app://auth/callback
```

7. o app recebe o `handoff_token`
8. o app chama:

```text
POST /auth/mobile/session
```

9. o backend cria a sessao do RendaTop e devolve o cookie `jwt`
10. o app persiste a sessao localmente com seguranca

## O que ja foi implementado

### Backend

- `GET /auth/google/login?client=mobile`
- `GET /auth/google/callback`
- `GET /auth/microsoft/login?client=mobile`
- `GET /auth/microsoft/callback`
- `POST /auth/mobile/session`
- handoff temporario em Redis
- redirect para deeplink mobile

### Mobile Android

- login Google via `WebAuthenticator`
- login Microsoft via `WebAuthenticator`
- callback Android para:

```text
br.com.rendatop.app://auth/callback
```

- troca do `handoff_token` por sessao real
- persistencia segura do cookie `jwt`

## Variaveis de ambiente do backend

Configure no backend:

```bash
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
GOOGLE_REDIRECT_URI=https://SEU_BACKEND/auth/google/callback
GOOGLE_FRONTEND_LOGIN_URL=https://SEU_FRONTEND/login

MICROSOFT_CLIENT_ID=
MICROSOFT_CLIENT_SECRET=
MICROSOFT_TENANT_ID=common
MICROSOFT_REDIRECT_URI=https://SEU_BACKEND/auth/microsoft/callback
MICROSOFT_FRONTEND_LOGIN_URL=https://SEU_FRONTEND/login

SSO_MOBILE_REDIRECT_URI=br.com.rendatop.app://auth/callback
COOKIE_SECURE=true
```

### Observacoes

- `GOOGLE_REDIRECT_URI` e `MICROSOFT_REDIRECT_URI` precisam apontar para o backend
- `SSO_MOBILE_REDIRECT_URI` e o deeplink que devolve o controle ao app
- `COOKIE_SECURE=true` deve ser usado em producao com HTTPS

## Google Cloud

### 1. Selecionar o projeto

No `Google Cloud Console`, selecione o projeto do RendaTop.

### 2. OAuth consent screen

Em `APIs & Services > OAuth consent screen`:

1. confira se o app esta publicado ou em modo de teste
2. preencha nome, email de suporte e dominios autorizados
3. confirme os escopos:
   - `openid`
   - `email`
   - `profile`

### 3. OAuth Client

Em `APIs & Services > Credentials`:

1. crie ou edite um `OAuth Client ID`
2. use o tipo:

```text
Web application
```

3. adicione em `Authorized redirect URIs`:

```text
https://SEU_BACKEND/auth/google/callback
```

4. copie:
   - `Client ID`
   - `Client Secret`

5. salve em:
   - `GOOGLE_CLIENT_ID`
   - `GOOGLE_CLIENT_SECRET`

## Microsoft Entra ID

### 1. Abrir o app registration

No `Microsoft Entra admin center`:

1. acesse `Identity > Applications > App registrations`
2. crie ou abra o app do RendaTop

### 2. Authentication

Em `Authentication`:

1. adicione uma plataforma `Web`
2. em `Redirect URIs`, cadastre:

```text
https://SEU_BACKEND/auth/microsoft/callback
```

3. confirme que `ID tokens` pode ser usado no fluxo, se o portal pedir essa opcao para OpenID Connect

### 3. Certificates & secrets

Em `Certificates & secrets`:

1. crie um `Client secret`
2. copie o valor gerado
3. salve em:

```bash
MICROSOFT_CLIENT_SECRET=
```

### 4. Overview

Copie tambem:

- `Application (client) ID` -> `MICROSOFT_CLIENT_ID`
- `Directory (tenant) ID` -> `MICROSOFT_TENANT_ID`

Se quiser aceitar contas de varios tenants, use:

```bash
MICROSOFT_TENANT_ID=common
```

### 5. API permissions

Em `API permissions`, confirme permissoes delegadas para:

- `openid`
- `profile`
- `email`
- `User.Read`

Na maior parte dos casos, isso aparece dentro de `Microsoft Graph`.

### 6. Salvar no backend

No backend, configure:

```bash
MICROSOFT_CLIENT_ID=
MICROSOFT_CLIENT_SECRET=
MICROSOFT_TENANT_ID=
MICROSOFT_REDIRECT_URI=https://SEU_BACKEND/auth/microsoft/callback
```

## Importante: nao precisa de client OAuth Android nem app registration mobile nativo

Neste desenho, Google e Microsoft redirecionam para o **backend web**, nao direto para o app.

Entao o essencial e:

- callback HTTP/HTTPS do backend cadastrado no provedor
- deeplink mobile configurado no app

Um client Android nativo ou fluxo mobile puro so seria necessario se o app deixasse de usar o handoff pelo backend.

## O que conferir no app Android

O app escuta:

```text
br.com.rendatop.app://auth/callback
```

Se isso mudar, alinhe 3 lugares:

1. `mobile/RendaTop.App/Services/AppConfig.cs`
2. `mobile/RendaTop.App/Platforms/Android/WebAuthenticationCallbackActivity.cs`
3. `SSO_MOBILE_REDIRECT_URI` no backend

## Teste manual recomendado

### Google

1. suba o backend com variaveis corretas
2. abra o app
3. toque em `Login com Google / GMail`
4. conclua o login
5. confirme retorno ao app
6. confirme navegacao para o dashboard

### Microsoft

1. suba o backend com variaveis corretas
2. abra o app
3. toque em `Entrar com a Microsoft / Outlook`
4. conclua o login
5. confirme retorno ao app
6. confirme navegacao para o dashboard

## Ressalvas importantes

### 1. O mobile depende do backend

Se o backend estiver sem client id, secret ou redirect corretos, o login social nao vai fechar.

### 2. O deeplink precisa bater exatamente

Se o backend redirecionar para outro URI, o app nao recebe o callback.

### 3. O cookie do navegador nao serve como sessao do app

Mesmo com login no navegador externo, o app precisa sempre trocar o `handoff_token` em:

```text
POST /auth/mobile/session
```

### 4. iOS ainda nao foi ligado

O backend ja esta pronto para o mesmo conceito de handoff, mas a versao iOS ainda vai precisar registrar o callback no target Apple.

## Arquivos principais alterados

- `server/Controllers/LoginController.cs`
- `server/Middlewares/AuthenticationMiddleware.cs`
- `mobile/RendaTop.App/Services/AuthService.cs`
- `mobile/RendaTop.App/Services/AppConfig.cs`
- `mobile/RendaTop.App/Models/AuthModels.cs`
- `mobile/RendaTop.App/Pages/LoginPage.xaml`
- `mobile/RendaTop.App/Pages/LoginPage.xaml.cs`
- `mobile/RendaTop.App/Platforms/Android/WebAuthenticationCallbackActivity.cs`
