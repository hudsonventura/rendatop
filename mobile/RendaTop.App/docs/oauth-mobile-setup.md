# OAuth mobile setup

O app Android abre os fluxos web atuais do backend:

- `GET /auth/google/login`
- `GET /auth/microsoft/login`

Isso reaproveita o que ja existe no servidor, sem alterar backend. A limitacao importante e que o login social atual cria o cookie no fluxo web e redireciona para o frontend web. Sem um handoff mobile ou callback por deeplink, o app MAUI nao recebe uma sessao nativa confiavel para chamar a API.

## Google Cloud

Configure no backend:

```bash
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
GOOGLE_REDIRECT_URI=https://SEU_DOMINIO/auth/google/callback
SSO_FRONTEND_LOGIN_URL=https://SEU_FRONTEND/login
```

No Google Cloud Console:

1. Abra APIs & Services > Credentials.
2. Crie ou edite um OAuth Client ID do tipo Web application.
3. Adicione `https://SEU_DOMINIO/auth/google/callback` em Authorized redirect URIs.
4. Garanta que o OAuth consent screen permita os escopos `openid`, `email` e `profile`.
5. Copie Client ID e Client Secret para `GOOGLE_CLIENT_ID` e `GOOGLE_CLIENT_SECRET`.

## Microsoft Entra

Configure no backend:

```bash
MICROSOFT_CLIENT_ID=
MICROSOFT_CLIENT_SECRET=
MICROSOFT_TENANT_ID=common
MICROSOFT_REDIRECT_URI=https://SEU_DOMINIO/auth/microsoft/callback
SSO_FRONTEND_LOGIN_URL=https://SEU_FRONTEND/login
```

No Microsoft Entra:

1. Abra App registrations e crie ou edite o app do RendaTop.
2. Em Authentication, adicione uma plataforma Web.
3. Cadastre `https://SEU_DOMINIO/auth/microsoft/callback` como Redirect URI.
4. Em Certificates & secrets, crie um client secret e salve em `MICROSOFT_CLIENT_SECRET`.
5. Em API permissions, confirme permissao para `openid`, `profile`, `email` e `User.Read`.

## Recomendacao para login social nativo

Para o app Android receber sessao propria sem depender do navegador, o backend deve oferecer um destes caminhos em uma etapa futura:

- callback por deeplink, por exemplo `br.com.rendatop.app://auth/callback`, com token de handoff de curta duracao;
- endpoint mobile que troca um authorization code por cookie/token do RendaTop;
- endpoint de handoff que cria a sessao no Redis e devolve o cookie `jwt` ao app.

Enquanto isso nao existir, email/senha, TOTP, cadastro e verificacao por email sao os fluxos nativos confiaveis no Android.
