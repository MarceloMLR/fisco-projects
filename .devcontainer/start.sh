#!/usr/bin/env bash
# Starts the demonstration in a codespace (ADR-0113), each time someone opens it: the local database, the API with the
# screens on port 5175 and the demonstration data that is missing. Then it makes the address public, shows it and
# keeps the log of the API in this terminal, which also keeps the codespace from stopping while it is used.
set -euo pipefail
cd "$(dirname "$0")/.."

address="https://${CODESPACE_NAME:-localhost}-5175.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}"
if curl -fsS -o /dev/null http://localhost:5175/health 2> /dev/null; then
  echo "A demonstração já está no ar: $address"
  exit 0
fi

echo "Subindo o banco, a API e os dados de demonstração. Leva alguns minutos..."
docker compose up -d --wait
log="${TMPDIR:-/tmp}/fiscoprojects-api.log"
dotnet run --project src/FiscoProjects.Api > "$log" 2>&1 &
api=$!
until curl -fsS -o /dev/null http://localhost:5175/health 2> /dev/null; do
  if ! kill -0 "$api" 2> /dev/null; then
    tail -n 40 "$log"
    echo "A API não subiu; o que ela escreveu está acima."
    exit 1
  fi
  sleep 2
done
(cd web && npm run demo:data)

shared="O link já é público: quem o tiver abre sem conta no GitHub."
if [ -z "${CODESPACE_NAME:-}" ] || ! gh codespace ports visibility 5175:public -c "$CODESPACE_NAME" > /dev/null 2>&1; then
  shared="Para compartilhar: aba Ports, botão direito na 5175, Port Visibility, Public."
fi
cat << EOF

==================================================================
 FiscoProjects no ar: $address
 $shared
 Entre com paulo.nogueira@empresa.example (administrador)
 ou marina.costa@empresa.example (gerente), sem senha.
==================================================================

EOF
tail -n 0 -f --pid="$api" "$log"
wait "$api"
