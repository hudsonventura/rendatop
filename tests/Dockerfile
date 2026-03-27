FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

COPY rendatop.slnx ./
COPY tests/tests.csproj tests/
COPY server/server.csproj server/

RUN dotnet restore tests/tests.csproj

COPY tests/ tests/
COPY server/ server/

RUN find tests server \( -type d -name bin -o -type d -name obj \) -prune -exec rm -rf {} +

ENTRYPOINT ["dotnet", "test", "tests/tests.csproj", "-c", "Release", "--no-restore", "--logger", "console;verbosity=normal"]
