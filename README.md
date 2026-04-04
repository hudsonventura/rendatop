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