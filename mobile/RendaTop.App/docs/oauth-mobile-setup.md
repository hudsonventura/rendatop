# Google login no app mobile

Este documento cobre o fluxo de login com Google que agora existe entre:

- app mobile MAUI Android
- backend do RendaTop
- Google OAuth

## Como o fluxo funciona

O app **nao fala com o Google diretamente** para criar a sessao do RendaTop.

O caminho e este:

1. o app abre `GET /auth/google/login?client=mobile`
2. o backend redireciona para o Google
3. o Google volta para `GET /auth/google/callback`
4. o backend identifica que o cliente e `mobile`
5. o backend cria um **handoff token temporario**
6. o backend redireciona para o deeplink do app:

```text
br.com.rendatop.app://auth/callback
```

7. o app recebe o `handoff_token`
8. o app chama:

```text
POST /auth/mobile/session
```

9. o backend cria a sessao real do RendaTop e devolve o cookie `jwt`
10. o app persiste esse cookie com seguranca e segue autenticado

Esse desenho evita depender do cookie do navegador externo, que nao e compartilhado com o `HttpClient` do app.

## O que ja foi implementado

### Backend

- `GET /auth/google/login?client=mobile`
- `GET /auth/google/callback`
- `POST /auth/mobile/session`
- handoff temporario em Redis
- redirect para o deeplink do app quando o login vier do mobile

### Mobile Android

- botao Google no login usando `WebAuthenticator`
- callback Android para:

```text
br.com.rendatop.app://auth/callback
```

- troca do `handoff_token` por sessao real no backend
- persistencia segura do cookie `jwt`

## Variaveis de ambiente do backend

Configure no backend:

```bash
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
GOOGLE_REDIRECT_URI=https://SEU_BACKEND/auth/google/callback
GOOGLE_MOBILE_REDIRECT_URI=br.com.rendatop.app://auth/callback
SSO_FRONTEND_LOGIN_URL=https://SEU_FRONTEND/login
COOKIE_SECURE=true
```

### Observacoes

- `GOOGLE_REDIRECT_URI` precisa apontar para o **callback HTTP/HTTPS do backend**
- `GOOGLE_MOBILE_REDIRECT_URI` e o deeplink que o backend usa para voltar ao app
- `COOKIE_SECURE=true` deve ser usado em producao com HTTPS

## O que configurar no Google Cloud

### 1. Abrir o projeto correto

No `Google Cloud Console`, selecione o projeto do RendaTop.

### 2. Consent Screen

Em `APIs & Services > OAuth consent screen`:

1. confira se o app esta publicado ou em modo de teste
2. preencha nome, email de suporte e dominios autorizados
3. confirme os escopos:
   - `openid`
   - `email`
   - `profile`

### 3. Credenciais OAuth

Em `APIs & Services > Credentials`:

1. crie ou edite um `OAuth Client ID`
2. use o tipo:

```text
Web application
```

3. em `Authorized redirect URIs`, adicione:

```text
https://SEU_BACKEND/auth/google/callback
```

4. copie:
   - `Client ID`
   - `Client Secret`

5. salve esses valores em:
   - `GOOGLE_CLIENT_ID`
   - `GOOGLE_CLIENT_SECRET`

## Importante: voce nao precisa de OAuth Client do tipo Android neste fluxo

Como o app usa o backend como intermediario, o Google redireciona para o **backend web**, nao direto para o app.

Entao, para este fluxo especifico, o essencial e:

- backend callback HTTP/HTTPS cadastrado no Google
- deeplink mobile configurado no app

Um client OAuth do tipo Android so seria obrigatorio se o app passasse a autenticar **diretamente com o Google**, sem handoff pelo backend.

## O que conferir no app Android

O app ja esta preparado para o callback:

```text
br.com.rendatop.app://auth/callback
```

Se no futuro voce mudar esse esquema/host/path, precisa alinhar 3 lugares:

1. `mobile/RendaTop.App/Services/AppConfig.cs`
2. `mobile/RendaTop.App/Platforms/Android/WebAuthenticationCallbackActivity.cs`
3. `GOOGLE_MOBILE_REDIRECT_URI` no backend

## Teste manual recomendado

1. suba o backend com as variaveis configuradas
2. abra o app Android
3. toque em `Login com Google / GMail`
4. conclua o login no navegador/custom tab
5. confirme que o app voltou sozinho
6. confirme que a tela navegou para o dashboard
7. feche e reabra o app para garantir que a sessao ficou persistida

## Ressalvas importantes

### 1. Google login no mobile depende do backend

Se o backend estiver sem:

- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`
- `GOOGLE_REDIRECT_URI`

o login social nao vai fechar.

### 2. Deeplink mobile precisa bater exatamente

Se o backend redirecionar para um URI diferente do que o app escuta, o login vai concluir no Google, mas o app nao vai receber o callback.

### 3. Cookie do navegador nao e a sessao do app

Mesmo que o Google login abra no navegador, o app precisa sempre trocar o `handoff_token` por sessao propria em:

```text
POST /auth/mobile/session
```

### 4. iOS ainda nao foi ligado

O backend ja ficou pronto para o mesmo conceito de handoff, mas o app iOS ainda vai precisar:

- registrar o mesmo deeplink no target iOS
- plugar o callback da plataforma
- validar o retorno no fluxo MAUI para iPhone/iPad

## Arquivos principais alterados

- `server/Controllers/LoginController.cs`
- `server/Middlewares/AuthenticationMiddleware.cs`
- `mobile/RendaTop.App/Services/AuthService.cs`
- `mobile/RendaTop.App/Services/AppConfig.cs`
- `mobile/RendaTop.App/Models/AuthModels.cs`
- `mobile/RendaTop.App/Pages/LoginPage.xaml`
- `mobile/RendaTop.App/Pages/LoginPage.xaml.cs`
- `mobile/RendaTop.App/Platforms/Android/WebAuthenticationCallbackActivity.cs`
