#!/usr/bin/env bash

set -euo pipefail

PROJECT_FILE="mobile/RendaTop.App/RendaTop.App.csproj"
KEYSTORE_PATH="${HOME}/source/rendatop/rendatop-release.keystore"

if [[ ! -f "$PROJECT_FILE" ]]; then
  echo "Projeto nao encontrado: $PROJECT_FILE" >&2
  exit 1
fi

if [[ -z "${SENHA:-}" ]]; then
  echo "Defina a variavel SENHA antes de executar o script." >&2
  echo 'Exemplo: export SENHA="minha-senha"' >&2
  exit 1
fi

if [[ ! -f "$KEYSTORE_PATH" ]]; then
  echo "Keystore nao encontrado em: $KEYSTORE_PATH" >&2
  exit 1
fi

display_version="$(sed -n 's:.*<ApplicationDisplayVersion>\(.*\)</ApplicationDisplayVersion>.*:\1:p' "$PROJECT_FILE" | head -n 1)"
application_version="$(sed -n 's:.*<ApplicationVersion>\(.*\)</ApplicationVersion>.*:\1:p' "$PROJECT_FILE" | head -n 1)"

if [[ -z "$display_version" || -z "$application_version" ]]; then
  echo "Nao foi possivel localizar ApplicationDisplayVersion/ApplicationVersion em $PROJECT_FILE" >&2
  exit 1
fi

IFS='.' read -r major minor patch <<< "$display_version"
major="${major:-0}"
minor="${minor:-0}"
patch="${patch:-0}"

if [[ ! "$major" =~ ^[0-9]+$ || ! "$minor" =~ ^[0-9]+$ || ! "$patch" =~ ^[0-9]+$ ]]; then
  echo "ApplicationDisplayVersion invalida: $display_version" >&2
  exit 1
fi

if [[ ! "$application_version" =~ ^[0-9]+$ ]]; then
  echo "ApplicationVersion invalida: $application_version" >&2
  exit 1
fi

new_patch=$((patch + 1))
new_application_version=$((application_version + 1))
new_display_version="${major}.${minor}.${new_patch}"

sed -i \
  -e "s:<ApplicationDisplayVersion>${display_version}</ApplicationDisplayVersion>:<ApplicationDisplayVersion>${new_display_version}</ApplicationDisplayVersion>:" \
  -e "s:<ApplicationVersion>${application_version}</ApplicationVersion>:<ApplicationVersion>${new_application_version}</ApplicationVersion>:" \
  "$PROJECT_FILE"

echo "Nova ApplicationDisplayVersion: $new_display_version"
echo "Nova ApplicationVersion: $new_application_version"

dotnet publish mobile/RendaTop.App/RendaTop.App.csproj \
  -f net10.0-android \
  -c Release \
  -p:AndroidPackageFormat=aab \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="$KEYSTORE_PATH" \
  -p:AndroidSigningStorePass="${SENHA}" \
  -p:AndroidSigningKeyAlias=rendatop \
  -p:AndroidSigningKeyPass="${SENHA}"
