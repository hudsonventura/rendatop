# dbclone

Console app em `.NET 10` para clonar um banco PostgreSQL de origem para um banco de destino.

O fluxo executado e:

1. Carrega as configuracoes do `.env`.
2. Gera um dump temporario do banco de origem.
3. Le a lista de tabelas com dados e conta as tuplas na origem.
4. Derruba e recria o banco de destino.
5. Restaura `pre-data`, `data` e `post-data`.
6. Registra tudo via `ILogger`.

## Variaveis `.env`

Origem:

```env
SOURCE_POSTGRES_HOST=localhost
SOURCE_POSTGRES_PORT=5432
SOURCE_POSTGRES_DB=rendatop_prod
SOURCE_POSTGRES_USER=postgres
SOURCE_POSTGRES_PASSWORD=senha
SOURCE_POSTGRES_MAINTENANCE_DB=postgres
```

Destino:

```env
TARGET_POSTGRES_HOST=localhost
TARGET_POSTGRES_PORT=5432
TARGET_POSTGRES_DB=rendatop_tests
TARGET_POSTGRES_USER=postgres
TARGET_POSTGRES_PASSWORD=senha
TARGET_POSTGRES_MAINTENANCE_DB=postgres
```

Observacoes:

- A origem aceita fallback para `POSTGRES_*`, caso voce queira reaproveitar as variaveis atuais do backend.
- O destino deve ser informado com `TARGET_POSTGRES_*`.
- O host que executa o app precisa ter `pg_dump` e `pg_restore` instalados.

## Pre-requisitos

O `dbclone` executa `pg_dump` e `pg_restore` diretamente na maquina host. Nao basta o PostgreSQL existir no Docker; esses binarios precisam estar instalados no sistema operacional.

No Ubuntu, instale com:

```bash
sudo apt update
sudo apt install postgresql-client
```

Se quiser alinhar com a mesma versao principal usada no ambiente dev atual, instale:

```bash
sudo apt update
sudo apt install postgresql-client-18
```

Depois valide:

```bash
pg_dump --version
pg_restore --version
```

Referencias oficiais:

- https://www.postgresql.org/download/
- https://www.postgresql.org/download/linux/ubuntu/
- https://wiki.postgresql.org/wiki/Apt

## Execucao

```bash
dotnet run --project dbclone/dbclone.csproj
```
