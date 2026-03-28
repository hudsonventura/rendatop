FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

COPY . .

RUN dotnet restore ./tests/tests.csproj
RUN dotnet build ./tests/tests.csproj -c Release --no-restore

ENTRYPOINT ["dotnet", "test", "./tests/tests.csproj", "-c", "Release", "--no-build"]