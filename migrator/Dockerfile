FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY migrator/migrator.csproj migrator/
COPY server/server.csproj server/

RUN dotnet restore migrator/migrator.csproj

COPY migrator/ migrator/
COPY server/ server/

RUN dotnet publish migrator/migrator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "migrator.dll"]
