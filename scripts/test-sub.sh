#!/usr/bin/env bash
#
# Simulate a FreeRADIUS client (real PPPoE router / access concentrator) against
# the running freeradius server, using the persistent isp-radclient container.
#
# THE EASY WAY TO TEST PPPoE CREDENTIALS:
# 	./scripts/test-sub.sh --up                     # start the client container (once)
#	./scripts/test-sub.sh <username> <password>    # test credentials (PAP)
#	./scripts/test-sub.sh --chap <user> <pass>     # test via CHAP
#	./scripts/test-sub.sh --down                   # stop the client when done
#
# It sends an Access-Request FROM 172.22.0.200 (registered NAS "sim-client",
# secret testing123) — the same source-IP + shared-secret path a real NAS uses.
#
# The sim-client container ("isp-radclient") must exist and be running on the
# compose network. If it is not running, run: ./scripts/test-sub.sh --up
#
# Verdicts (same as a real NAS sees):
#   Access-Accept  -> credentials valid (subscription active + password right)
#   Access-Reject  -> NAS auth OK but password wrong / subscription not active
#   No reply       -> something dropped it (check freeradius log)
#
# Options:
#   --up        start the persistent isp-radclient container
#   --down      stop and remove the isp-radclient container
#   --chap      use CHAP instead of PAP (some PPPoE routers use CHAP)
#
set -euo pipefail
cd "$(dirname "$0")/.."

# Use the compose DNS name so we're immune to container IP churn across restarts.
# The sim-client and freeradius are on the same default compose network, so
# "freeradius" resolves inside the sim-client container.
FREERADIUS_HOST=freeradius
FREERADIUS_PORT=1812
CLIENT_IP=172.22.0.200
CONTAINER=isp-radclient

# The NAS secret is whatever is stored in the `nas` table for CLIENT_IP.
# Resolve it live so the script always signs with the current value (no drift).
# You can override it with the TEST_NSECRET env var if you know better.
resolve_nas_secret() {
    CLIENT_SECRET=$(docker compose exec -T postgres psql -U radius -d isp_manager -tA -c \
        "SELECT secret FROM nas WHERE nasname = '${CLIENT_IP}';" < /dev/null 2>/dev/null | tr -d '[:space:]')
    if [ -z "$CLIENT_SECRET" ]; then
        CLIENT_SECRET="${TEST_NSECRET:-testing123}"
    fi
}

# ── State commands ────────────────────────────────────────────────
# By default this is READ-ONLY: it looks up the sim-client's NAS row and
# returns its secret (so the script can sign requests correctly).  If the row
# is missing and the caller passes "ask" ($1 == ask), it prompts whether to
# re-register it — "n/no" continues read-only and the call proceeds anyway.
ensure_nas_client() {
    local row
    row=$(docker compose exec -T postgres psql -U radius -d isp_manager -tA -c \
        "SELECT secret FROM nas WHERE nasname = '${CLIENT_IP}';" < /dev/null 2>/dev/null | tr -d '[:space:]')
    if [ -z "$row" ]; then
        echo "WARN: no NAS row for ${CLIENT_IP} exists in the nas table."
        echo "      FreeRADIUS will drop requests from this IP (unknown client)."
        if [ "${1:-}" = "ask" ]; then
            local answer
            printf 'Insert the sim-client NAS row now? [y/N] '
            read -r answer || true
            case "$answer" in
                y|Y|yes|YES|Yes)
                    resolve_nas_secret
                    if docker compose exec -T postgres psql -U radius -d isp_manager -c \
                        "INSERT INTO nas (nasname, shortname, type, secret) VALUES ('${CLIENT_IP}','sim-client','other','${CLIENT_SECRET}') ON CONFLICT (nasname) DO NOTHING;" < /dev/null >/dev/null 2>&1; then
                        echo "Registered sim-client NAS row (${CLIENT_IP} / ${CLIENT_SECRET})."
                        echo "Reloading freeradius so it picks up the client..."
                        docker compose restart freeradius >/dev/null 2>&1
                        sleep 4
                    else
                        echo "WARN: could not register NAS row (is postgres up?)." >&2
                    fi
                    ;;
                *)
                    echo "OK — continuing read-only without inserting."
                    ;;
            esac
        else
            echo "      Run: ./scripts/test-sub.sh --up   (or edit the nas table) to register it."
        fi
        CLIENT_SECRET="${TEST_NSECRET:-testing123}"
    else
        CLIENT_SECRET="$row"
    fi
}

if [ "${1:-}" = "--up" ]; then
    if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER}$"; then
        echo "sim-client already exists: starting it."
        docker start "${CONTAINER}" >/dev/null
    else
        echo "Creating persistent sim-client ${CONTAINER} (source ${CLIENT_IP})..."
        docker run -d --name "${CONTAINER}" --restart unless-stopped \
            --network internet_provider_system_default --ip "${CLIENT_IP}" \
            --entrypoint sh freeradius/freeradius-server:latest \
            -c 'trap : TERM INT; sleep infinity & wait' >/dev/null
    fi
    ensure_nas_client ask
    echo "Using NAS secret from table: ${CLIENT_SECRET}"
    echo "sim-client running at ${CLIENT_IP} -> freeradius (compose DNS '${FREERADIUS_HOST}')"
    exit 0
fi

if [ "${1:-}" = "--down" ]; then
    docker rm -f "${CONTAINER}" >/dev/null 2>&1 || true
    echo "sim-client removed."
    exit 0
fi

# ── Check the NAS client is registered (may prompt yes/no to insert) ─
ensure_nas_client ask

# ── Use the NAS secret stored in the `nas` table (current value) ──
resolve_nas_secret

# ── Ensure container is up ────────────────────────────────────────
if ! docker ps --format '{{.Names}}' | grep -q "^${CONTAINER}$"; then
    echo "sim-client container '${CONTAINER}' is not running." >&2
    echo "Start it first: ./scripts/test-sub.sh --up" >&2
    exit 3
fi

# ── Parse mode and credentials ────────────────────────────────────
CHAP_MODE=0
args=()
while [ "$#" -gt 0 ]; do
    case "$1" in
        --chap) CHAP_MODE=1; shift ;;
        *) args+=("$1"); shift ;;
    esac
done

if [ "${#args[@]}" -lt 2 ]; then
    echo "Usage: $0 <username> <password> [--chap]" >&2
    echo "       $0 --up | --down" >&2
    exit 2
fi
RADIUS_USER="${args[0]}"
RADIUS_PASS="${args[1]}"

echo "Simulating NAS client ${CLIENT_IP} -> ${FREERADIUS_HOST}:${FREERADIUS_PORT}"
echo "  credentials: ${RADIUS_USER} / ${RADIUS_PASS}"
echo "  method:      $([ "$CHAP_MODE" = 1 ] && echo CHAP || echo PAP)"

# ── Send the request from inside the sim-client container ─────────
if [ "$CHAP_MODE" = 1 ]; then
    # CHAP: build a radclient request on the host, feed it into the container
    REQ=$(python3 scripts/chap-gen.py "$RADIUS_USER" "$RADIUS_PASS")
    OUT=$(printf 'User-Name = "%s"\n%s\nNAS-IP-Address = %s\nNAS-Port = 0\n' \
        "$RADIUS_USER" "$(echo "$REQ" | grep -E 'CHAP')" "$CLIENT_IP" \
        | docker exec -i "${CONTAINER}" radclient -x "${FREERADIUS_HOST}:${FREERADIUS_PORT}" auth "${CLIENT_SECRET}" 2>&1 || true)
else
    OUT=$(docker exec "${CONTAINER}" sh -c \
        "printf 'User-Name = \"${RADIUS_USER}\"\nUser-Password = \"${RADIUS_PASS}\"\nNAS-IP-Address = ${CLIENT_IP}\nNAS-Port = 0\n' \
         | radclient -x ${FREERADIUS_HOST}:${FREERADIUS_PORT} auth ${CLIENT_SECRET}" 2>&1 || true)
fi

echo "$OUT" | grep -E "Sent Access|Received|Rad" | head -6

if echo "$OUT" | grep -q "Received Access-Accept"; then
    echo ""
    echo ">>> Access-Accept — credentials are VALID."
    exit 0
elif echo "$OUT" | grep -q "Received Access-Reject"; then
    echo ""
    echo ">>> Access-Reject — NAS recognized but authentication FAILED (bad password / subscription not active)."
    exit 1
else
    echo ""
    echo ">>> No reply — request dropped (secret mismatch or server unreachable)."
    exit 2
fi