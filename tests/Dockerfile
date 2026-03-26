FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

COPY rendatop.slnx ./
COPY tests/tests.csproj tests/
COPY server/server.csproj server/

RUN dotnet restore tests/tests.csproj

COPY tests/ tests/
COPY server/ server/

ENTRYPOINT ["dotnet", "test", "tests/tests.csproj", "-c", "Release", "--no-restore", "--logger", "console;verbosity=normal"]
