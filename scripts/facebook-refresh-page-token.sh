#!/usr/bin/env bash

set -euo pipefail

GRAPH_VERSION="${META_GRAPH_API_VERSION:-v23.0}"
APP_ID="${FACEBOOK_APP_ID:-}"
APP_SECRET="${FACEBOOK_APP_SECRET:-}"
USER_TOKEN="${FACEBOOK_USER_ACCESS_TOKEN:-}"
TOKEN_EXPLORER_URL="https://developers.facebook.com/tools/explorer/"

if [[ -z "$APP_ID" || -z "$APP_SECRET" || -z "$USER_TOKEN" ]]; then
  cat <<'EOF'
Uso:
  export FACEBOOK_APP_ID=... && \
  export FACEBOOK_APP_SECRET=... && \
  export FACEBOOK_USER_ACCESS_TOKEN=... && \
  bash scripts/facebook-refresh-page-token.sh

Variáveis aceitas:
  META_GRAPH_API_VERSION        opcional, default v23.0
  FACEBOOK_APP_ID               obrigatório
  FACEBOOK_APP_SECRET           obrigatório
  FACEBOOK_USER_ACCESS_TOKEN    obrigatório

O script:
1. troca o user token por um long-lived user token
2. lista as páginas retornadas em /me/accounts
3. imprime o PAGE_ID e PAGE_ACCESS_TOKEN prontos para copiar no .env

Para reemitir o short-lived user token manualmente:
  https://developers.facebook.com/tools/explorer/
EOF
  exit 1
fi

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Comando obrigatório não encontrado: $1" >&2
    exit 1
  fi
}

require_command curl
require_command python3

extract_error_field() {
  local field="$1"
  python3 - "$field" <<'PY'
import json
import sys

field = sys.argv[1]
payload = sys.stdin.read()
try:
    document = json.loads(payload)
except Exception:
    print("")
    raise SystemExit(0)

error = document.get("error") or {}
value = error.get(field, "")
if isinstance(value, bool):
    print("true" if value else "false")
else:
    print(value if value is not None else "")
PY
}

run_graph_get() {
  local url="$1"
  shift

  local max_attempts=3
  local attempt=1

  while (( attempt <= max_attempts )); do
    local tmp_body
    tmp_body="$(mktemp)"
    local http_code

    http_code="$(
      curl -sS \
        -o "$tmp_body" \
        -w "%{http_code}" \
        "$url" \
        --get \
        "$@"
    )"

    local body
    body="$(cat "$tmp_body")"
    rm -f "$tmp_body"

    if [[ "$http_code" =~ ^2 ]]; then
      printf "%s" "$body"
      return 0
    fi

    local error_code
    local is_transient
    error_code="$(extract_error_field "code" <<<"$body")"
    is_transient="$(extract_error_field "is_transient" <<<"$body")"

    if [[ "$http_code" =~ ^5|429$ || "$is_transient" == "true" || "$error_code" == "2" ]]; then
      if (( attempt < max_attempts )); then
        echo "Tentativa ${attempt}/${max_attempts} falhou para: $url" >&2
        echo "HTTP $http_code" >&2
        echo "$body" >&2
        echo "Erro transitório detectado. Tentando novamente..." >&2
        sleep "$attempt"
        ((attempt++))
        continue
      fi
    fi

    echo "Falha na chamada para: $url" >&2
    echo "HTTP $http_code" >&2
    echo "$body" >&2
    return 1
  done
}

echo "-> Trocando user token por long-lived user token..."

if ! LONG_LIVED_RESPONSE="$(
  run_graph_get "https://graph.facebook.com/${GRAPH_VERSION}/oauth/access_token" \
    --data-urlencode "grant_type=fb_exchange_token" \
    --data-urlencode "client_id=${APP_ID}" \
    --data-urlencode "client_secret=${APP_SECRET}" \
    --data-urlencode "fb_exchange_token=${USER_TOKEN}"
)"; then
  if [[ "$(extract_error_field "is_transient" <<<"${LONG_LIVED_RESPONSE:-}")" == "true" ]]; then
    cat >&2 <<EOF

O Facebook sinalizou erro transitório. Tente novamente em alguns minutos.
Se precisar reemitir o short-lived user token:
  ${TOKEN_EXPLORER_URL}
EOF
    exit 1
  fi
  cat >&2 <<EOF

Possíveis causas comuns:
- FACEBOOK_USER_ACCESS_TOKEN expirado
- FACEBOOK_APP_ID ou FACEBOOK_APP_SECRET não pertencem ao mesmo app do token
- token de usuário gerado para outro app Meta
- app em modo Development sem o usuário correto vinculado ao app
Se precisar reemitir o short-lived user token:
  ${TOKEN_EXPLORER_URL}
EOF
  exit 1
fi

LONG_LIVED_TOKEN="$(
  python3 -c 'import json,sys; print(json.load(sys.stdin).get("access_token",""))' <<<"$LONG_LIVED_RESPONSE"
)"

EXPIRES_IN="$(
  python3 -c 'import json,sys; print(json.load(sys.stdin).get("expires_in",""))' <<<"$LONG_LIVED_RESPONSE"
)"

if [[ -z "$LONG_LIVED_TOKEN" ]]; then
  echo "Não foi possível obter o long-lived user token." >&2
  echo "$LONG_LIVED_RESPONSE" >&2
  exit 1
fi

echo "-> Long-lived user token obtido."
if [[ -n "$EXPIRES_IN" ]]; then
  echo "   expires_in: ${EXPIRES_IN} segundos"
fi

echo
echo "-> Listando páginas disponíveis em /me/accounts..."

if ! PAGES_RESPONSE="$(
  run_graph_get "https://graph.facebook.com/${GRAPH_VERSION}/me/accounts" \
    --data-urlencode "access_token=${LONG_LIVED_TOKEN}"
)"; then
  if [[ "$(extract_error_field "is_transient" <<<"${PAGES_RESPONSE:-}")" == "true" ]]; then
    cat >&2 <<EOF

O Facebook sinalizou erro transitório. Tente novamente em alguns minutos.
Se precisar reemitir o short-lived user token:
  ${TOKEN_EXPLORER_URL}
EOF
    exit 1
  fi
  cat >&2 <<EOF

Possíveis causas comuns:
- o usuário do token não tem acesso à página
- faltam permissões como pages_manage_posts/pages_read_engagement
- o app/tipo de token não tem acesso a Pages
Se precisar reemitir o short-lived user token:
  ${TOKEN_EXPLORER_URL}
EOF
  exit 1
fi

python3 -c '
import json
import sys

long_lived_token = sys.argv[1]
payload = json.load(sys.stdin)
pages = payload.get("data", [])

print()
print("FACEBOOK_LONG_LIVED_USER_ACCESS_TOKEN=" + long_lived_token)
print()

if not pages:
    print("Nenhuma página encontrada para este usuário/token.")
    raise SystemExit(0)

for index, page in enumerate(pages, start=1):
    name = page.get("name", "")
    page_id = page.get("id", "")
    page_token = page.get("access_token", "")
    tasks = ",".join(page.get("tasks", []))

    print(f"[Página {index}] {name}")
    print(f"FACEBOOK_PAGE_ID={page_id}")
    print(f"FACEBOOK_PAGE_ACCESS_TOKEN={page_token}")
    if tasks:
        print(f"TASKS={tasks}")
    print()
' "$LONG_LIVED_TOKEN" <<<"$PAGES_RESPONSE"
