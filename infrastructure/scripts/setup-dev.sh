#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# HalalChain dev environment bootstrap
#
# This script brings up the full local blockchain + IPFS + API stack in one
# command. It is idempotent: re-running it is safe.
#
# What it does:
#   1. cd's to the repo root (absolute path) — fixes the "path drift" bug
#      that happened when the previous run did `cd contracts` first.
#   2. Checks for Foundry (forge/cast/anvil) — installs if missing and possible.
#   3. Pulls the two contract-library deps (forge-std, OpenZeppelin).
#   4. Brings up docker compose (postgres + kubo IPFS + Anvil Polygon fork).
#   5. Waits for Anvil to be reachable on :8545.
#   6. Deploys the 5 HalalChain contracts and parses the printed addresses.
#   7. Writes the addresses into appsettings.Development.json so the API
#      boots with the blockchain module enabled.
#   8. Prints a single, copy-pasteable "next steps" block.
#
# Usage:
#   bash infrastructure/scripts/setup-dev.sh
#
# Requirements:
#   - Docker + docker compose
#   - Foundry (forge/cast/anvil) — script will attempt install if missing
#   - curl, jq, sha256sum, awk, grep
#
# Idempotent: safe to re-run. If anything is already up, it stays up.
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

# ── Colors (disabled if not a tty) ────────────────────────────────────────
if [ -t 1 ]; then
    BOLD='\033[1m' GREEN='\033[0;32m' YELLOW='\033[1;33m' RED='\033[0;31m' NC='\033[0m'
else
    BOLD='' GREEN='' YELLOW='' RED='' NC=''
fi

info()  { echo -e "${GREEN}==>${NC} $*"; }
warn()  { echo -e "${YELLOW}!!${NC}  $*"; }
fail()  { echo -e "${RED}!!${NC}  $*" >&2; exit 1; }

# ── 1. Always start from the repo root (absolute path) ────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"
info "Repo root: $REPO_ROOT"

# ── 2. Check/install Foundry ─────────────────────────────────────────────
ensure_foundry() {
    if command -v forge >/dev/null 2>&1; then
        info "Foundry already installed: $(forge --version | head -1)"
        return 0
    fi

    info "Foundry not found; attempting install..."
    local install_dir="${FOUNDRY_HOME:-$HOME/.foundry}"
    mkdir -p "$install_dir" 2>/dev/null || {
        # Fallback to workspace-local path if home is read-only
        install_dir="$REPO_ROOT/.foundry"
        mkdir -p "$install_dir"
        warn "Home directory read-only; installing Foundry to $install_dir"
    }

    if [ ! -x "$install_dir/bin/foundryup" ]; then
        info "Downloading foundryup bootstrapper..."
        curl -sL https://foundry.paradigm.xyz | bash -s -- -y "$install_dir" 2>/dev/null || {
            # Fallback: manual download of foundryup
            local fu="$install_dir/bin/foundryup"
            mkdir -p "$install_dir/bin"
            curl -sL https://foundry.paradigm.xyz | bash -s -- -y "$install_dir" 2>/dev/null || {
                warn "Could not install Foundry automatically."
                warn "Install manually: curl -L https://foundry.paradigm.xyz | bash"
                warn "Then re-run this script."
                return 1
            }
        }
    fi

    info "Running foundryup to download forge/cast/anvil..."
    "$install_dir/bin/foundryup" -y "$install_dir" 2>&1 | tail -5 || {
        warn "foundryup failed. Trying alternative install..."
        "$install_dir/bin/foundryup" 2>&1 | tail -5 || return 1
    }

    export PATH="$install_dir/bin:$PATH"
    info "forge at: $(command -v forge)"
    return 0
}

if ! ensure_foundry; then
    fail "Foundry installation failed. Please install Foundry manually and re-run."
fi

# ── 3. Install contract dependencies ──────────────────────────────────────
info "Installing forge-std + OpenZeppelin contracts..."
CONTRACTS_DIR="$REPO_ROOT/HalalChain.Platform.Contracts/contracts"
cd "$CONTRACTS_DIR"

# Create .env for Foundry if missing (for RPC URLs in foundry.toml)
if [ ! -f ".env" ]; then
    cat > .env <<'ENVEOF'
# Foundry / Forge environment variables
# These are used by foundry.toml for RPC URLs and Etherscan keys.
# Populate these before running forge script --verify or mainnet deploys.

# Polygon Amoy (testnet)
POLYGON_AMOY_RPC_URL=https://rpc-amoy.polygon.technology

# Polygon Mainnet
POLYGON_MAINNET_RPC_URL=https://polygon-rpc.com

# Base Sepolia (testnet)
BASE_SEPOLIA_RPC_URL=https://sepolia.base.org

# Base Mainnet
BASE_MAINNET_RPC_URL=https://mainnet.base.org

# Etherscan / block explorer API keys (for contract verification)
POLYGONSCAN_API_KEY=
BASESCAN_API_KEY=
ENVEOF
    info "Created $CONTRACTS_DIR/.env (RPC URLs for foundry.toml)"
fi

# Load .env so foundry.toml env vars are available
set -a
source .env 2>/dev/null || true
set +a

[ -d "lib/forge-std" ] || forge install foundry-rs/forge-std --no-commit --no-git
[ -d "lib/openzeppelin-contracts" ] || forge install OpenZeppelin/openzeppelin-contracts@v5.0.2 --no-commit --no-git

info "Building contracts..."
forge build 2>&1 | tail -3

info "Running Foundry tests..."
forge test 2>&1 | tail -5 || warn "Some Foundry tests failed (continuing)"

# ── 4. Bring up docker compose ───────────────────────────────────────────
cd "$REPO_ROOT"
DOCKER_COMPOSE_FILE="infrastructure/docker-compose.dev.yml"
if [ ! -f "$DOCKER_COMPOSE_FILE" ]; then
    # Fallback: check if file exists at old location inside API project
    DOCKER_COMPOSE_FILE="HalalChain.Platform.Api/infrastructure/docker-compose.dev.yml"
    if [ ! -f "$DOCKER_COMPOSE_FILE" ]; then
        fail "Docker compose file not found at infrastructure/docker-compose.dev.yml or HalalChain.Platform.Api/infrastructure/docker-compose.dev.yml"
    fi
fi

info "Bringing up docker compose (postgres + kubo + anvil fork)..."
docker compose -f "$DOCKER_COMPOSE_FILE" --profile chain up -d

# ── 5. Wait for Anvil ────────────────────────────────────────────────────
info "Waiting for Anvil (127.0.0.1:8545)..."
ANVIL_RPC="http://127.0.0.1:8545"
for i in $(seq 1 60); do
    if curl -sf -X POST -H "Content-Type: application/json" \
         --data '{"jsonrpc":"2.0","method":"eth_blockNumber","params":[],"id":1}' \
         "$ANVIL_RPC" >/dev/null 2>&1; then
        BLOCK=$(curl -sf -X POST -H "Content-Type: application/json" \
            --data '{"jsonrpc":"2.0","method":"eth_blockNumber","params":[],"id":1}' \
            "$ANVIL_RPC" | grep -o '"result":"[^"]*"' | cut -d'"' -f4 || echo "?")
        info "Anvil is up (block $BLOCK)."
        break
    fi
    sleep 1
    if [ "$i" -eq 60 ]; then
        fail "Anvil didn't respond on $ANVIL_RPC after 60s. Try: docker compose -f $DOCKER_COMPOSE_FILE logs polygon-fork"
    fi
done

# Wait for kubo IPFS
info "Waiting for kubo IPFS API (127.0.0.1:5011)..."
KUBO_UP=false
for i in $(seq 1 60); do
    if curl -sf -X POST "http://127.0.0.1:5011/api/v0/id" >/dev/null 2>&1; then
        info "kubo is up."
        KUBO_UP=true
        break
    fi
    sleep 1
done
if [ "$KUBO_UP" = false ]; then
    warn "kubo didn't respond on :5011 after 60s. Continuing without IPFS."
fi

# ── 6. Deploy the contracts ─────────────────────────────────────────────
cd "$CONTRACTS_DIR"
info "Deploying HalalChain contracts to Anvil..."

# Anvil's first deterministic account (well-known dev key, only safe on a local fork)
export PRIVATE_KEY="0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80"

DEPLOY_OUTPUT=$(forge script script/Deploy.s.sol:Deploy \
    --rpc-url "$ANVIL_RPC" \
    --broadcast \
    --private-key "$PRIVATE_KEY" 2>&1) || {
    fail "Deploy failed. Check output above. Try: forge script script/Deploy.s.sol:Deploy --rpc-url $ANVIL_RPC --broadcast --private-key $PRIVATE_KEY"
}
echo "$DEPLOY_OUTPUT" | tail -20

# Parse the five contract addresses from the script output.
# `forge script` prints lines like "HalalAccessControl       : 0xABC..."
ADDR_ACCESS=$(echo "$DEPLOY_OUTPUT" | grep -E '^HalalAccessControl\s*:' | awk '{print $NF}')
ADDR_SUPPLIERS=$(echo "$DEPLOY_OUTPUT" | grep -E '^SupplierRegistry\s*:' | awk '{print $NF}')
ADDR_PRODUCTS=$(echo "$DEPLOY_OUTPUT" | grep -E '^HalalProductRegistry\s*:' | awk '{print $NF}')
ADDR_CERTS=$(echo "$DEPLOY_OUTPUT" | grep -E '^HalalCertificationRegistry\s*:' | awk '{print $NF}')
ADDR_EVENTS=$(echo "$DEPLOY_OUTPUT" | grep -E '^TraceabilityEventLog\s*:' | awk '{print $NF}')

# If parsing failed (e.g. broadcast never landed), fail loudly.
for label in ACCESS SUPPLIERS PRODUCTS CERTS EVENTS; do
    eval "val=\$ADDR_$label"
    if [ -z "$val" ] || [ "${val:0:2}" != "0x" ]; then
        fail "Failed to extract $label address from deploy output."
    fi
done

echo
info "Contract addresses:"
echo "    HalalAccessControl        : $ADDR_ACCESS"
echo "    SupplierRegistry          : $ADDR_SUPPLIERS"
echo "    HalalProductRegistry      : $ADDR_PRODUCTS"
echo "    HalalCertificationRegistry: $ADDR_CERTS"
echo "    TraceabilityEventLog      : $ADDR_EVENTS"

# ── 7. Write appsettings.Development.json for the API ───────────────────
cd "$REPO_ROOT"
SETTINGS_FILE="HalalChain.Platform.Api/appsettings.Development.json"
cat > "$SETTINGS_FILE" <<EOF
{
  "ConnectionStrings": {
    "Postgres": "",
    "Sqlite": "Data Source=halalchain.db"
  },
  "Blockchain": {
    "RpcUrl": "$ANVIL_RPC",
    "ChainId": 80002,
    "Confirmations": 1,
    "HotWalletKey": "$PRIVATE_KEY",
    "GasPriceCapGwei": 50,
    "TxTimeoutSeconds": 60,
    "OutboxPollSeconds": 5,
    "OutboxBatchSize": 10,
    "Contracts": {
      "AccessControl": "$ADDR_ACCESS",
      "Suppliers":     "$ADDR_SUPPLIERS",
      "Products":      "$ADDR_PRODUCTS",
      "Certs":         "$ADDR_CERTS",
      "Events":        "$ADDR_EVENTS"
    },
    "Abi": {
      "AccessControl": {
        "supportsInterface": "[{\"inputs\":[{\"name\":\"interfaceId\",\"type\":\"bytes4\"}],\"name\":\"supportsInterface\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]",
        "grantRole": "[{\"inputs\":[{\"name\":\"role\",\"type\":\"bytes32\"},{\"name\":\"account\",\"type\":\"address\"}],\"name\":\"grantRole\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]",
        "revokeRole": "[{\"inputs\":[{\"name\":\"role\",\"type\":\"bytes32\"},{\"name\":\"account\",\"type\":\"address\"}],\"name\":\"revokeRole\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]"
      },
      "Suppliers": {
        "registerSupplier": "[{\"inputs\":[{\"name\":\"supplierId\",\"type\":\"bytes32\"},{\"name\":\"wallet\",\"type\":\"address\"},{\"name\":\"ipfsMetadataCid\",\"type\":\"string\"},{\"name\":\"jurisdiction\",\"type\":\"string\"}],\"name\":\"registerSupplier\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"supplierId\",\"type\":\"bytes32\"}],\"name\":\"getSupplier\",\"outputs\":[{\"components\":[{\"name\":\"supplierId\",\"type\":\"bytes32\"},{\"name\":\"wallet\",\"type\":\"address\"},{\"name\":\"proposedWallet\",\"type\":\"address\"},{\"name\":\"ipfsMetadataCid\",\"type\":\"string\"},{\"name\":\"jurisdiction\",\"type\":\"string\"},{\"name\":\"registeredAt\",\"type\":\"uint64\"},{\"name\":\"updatedAt\",\"type\":\"uint64\"},{\"name\":\"status\",\"type\":\"uint8\"},{\"name\":\"exists\",\"type\":\"bool\"}],\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]",
        "setStatus": "[{\"inputs\":[{\"name\":\"supplierId\",\"type\":\"bytes32\"},{\"name\":\"newStatus\",\"type\":\"uint8\"}],\"name\":\"setStatus\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]",
        "supplierExists": "[{\"inputs\":[{\"name\":\"supplierId\",\"type\":\"bytes32\"}],\"name\":\"supplierExists\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]"
      },
      "Products": {
        "registerProduct": "[{\"inputs\":[{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"supplierId\",\"type\":\"bytes32\"},{\"name\":\"metadataHash\",\"type\":\"bytes32\"},{\"name\":\"ipfsCid\",\"type\":\"string\"},{\"name\":\"jurisdiction\",\"type\":\"string\"}],\"name\":\"registerProduct\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"productId\",\"type\":\"bytes32\"}],\"name\":\"getProduct\",\"outputs\":[{\"components\":[{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"supplierId\",\"type\":\"bytes32\"},{\"name\":\"metadataHash\",\"type\":\"bytes32\"},{\"name\":\"ipfsCid\",\"type\":\"string\"},{\"name\":\"jurisdiction\",\"type\":\"string\"},{\"name\":\"currentCertId\",\"type\":\"bytes32\"},{\"name\":\"registeredAt\",\"type\":\"uint64\"},{\"name\":\"updatedAt\",\"type\":\"uint64\"},{\"name\":\"status\",\"type\":\"uint8\"},{\"name\":\"exists\",\"type\":\"bool\"}],\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"certId\",\"type\":\"bytes32\"}],\"name\":\"setCurrentCertificate\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"productId\",\"type\":\"bytes32\"}],\"name\":\"productExists\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]",
        "setStatus": "[{\"inputs\":[{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"newStatus\",\"type\":\"uint8\"}],\"name\":\"setStatus\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]",
        "recall": "[{\"inputs\":[{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"reasonCid\",\"type\":\"string\"}],\"name\":\"recall\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]"
      },
      "Certs": {
        "issueCertificate": "[{\"inputs\":[{\"name\":\"certId\",\"type\":\"bytes32\"},{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"documentCid\",\"type\":\"string\"},{\"name\":\"expiresAt\",\"type\":\"uint64\"},{\"name\":\"scopeCid\",\"type\":\"string\"},{\"name\":\"country\",\"type\":\"string\"}],\"name\":\"issueCertificate\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"certId\",\"type\":\"bytes32\"}],\"name\":\"getCertificate\",\"outputs\":[{\"components\":[{\"name\":\"certId\",\"type\":\"bytes32\"},{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"certifier\",\"type\":\"address\"},{\"name\":\"documentCid\",\"type\":\"string\"},{\"name\":\"issuedAt\",\"type\":\"uint64\"},{\"name\":\"expiresAt\",\"type\":\"uint64\"},{\"name\":\"scopeCid\",\"type\":\"string\"},{\"name\":\"country\",\"type\":\"string\"},{\"name\":\"status\",\"type\":\"uint8\"},{\"name\":\"exists\",\"type\":\"bool\"}],\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"certId\",\"type\":\"bytes32\"}],\"name\":\"revokeCertificate\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"certId\",\"type\":\"bytes32\"}],\"name\":\"expireCertificate\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"certId\",\"type\":\"bytes32\"}],\"name\":\"isValid\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]",
        "revokeCertificate": "[{\"inputs\":[{\"name\":\"certId\",\"type\":\"bytes32\"},{\"name\":\"reasonCid\",\"type\":\"string\"}],\"name\":\"revokeCertificate\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]"
      },
      "Events": {
        "recordEvent": "[{\"inputs\":[{\"name\":\"eventId\",\"type\":\"bytes32\"},{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"batchId\",\"type\":\"bytes32\"},{\"name\":\"eventType\",\"type\":\"uint8\"},{\"name\":\"locationCid\",\"type\":\"string\"},{\"name\":\"evidenceCid\",\"type\":\"string\"},{\"name\":\"notesCid\",\"type\":\"string\"}],\"name\":\"recordEvent\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"eventId\",\"type\":\"bytes32\"}],\"name\":\"getEvent\",\"outputs\":[{\"components\":[{\"name\":\"eventId\",\"type\":\"bytes32\"},{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"batchId\",\"type\":\"bytes32\"},{\"name\":\"actor\",\"type\":\"address\"},{\"name\":\"eventType\",\"type\":\"uint8\"},{\"name\":\"locationCid\",\"type\":\"string\"},{\"name\":\"evidenceCid\",\"type\":\"string\"},{\"name\":\"notesCid\",\"type\":\"string\"},{\"name\":\"timestamp\",\"type\":\"uint64\"},{\"name\":\"exists\",\"type\":\"bool\"}],\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"productId\",\"type\":\"bytes32\"}],\"name\":\"getEventsForProduct\",\"outputs\":[{\"components\":[{\"name\":\"eventId\",\"type\":\"bytes32\"},{\"name\":\"productId\",\"type\":\"bytes32\"},{\"name\":\"batchId\",\"type\":\"bytes32\"},{\"name\":\"actor\",\"type\":\"address\"},{\"name\":\"eventType\",\"type\":\"uint8\"},{\"name\":\"locationCid\",\"type\":\"string\"},{\"name\":\"evidenceCid\",\"type\":\"string\"},{\"name\":\"notesCid\",\"type\":\"string\"},{\"name\":\"timestamp\",\"type\":\"uint64\"},{\"name\":\"exists\",\"type\":\"bool\"}],\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]"
      }
    }
  },
  "IPFS": {
    "Provider": "kubo-local",
    "KuboApiUrl": "http://127.0.0.1:5011",
    "KuboGateway": "http://127.0.0.1:8081",
    "MaxFileBytes": 20971520,
    "AllowedMime": "image/jpeg,image/png,image/webp,application/pdf"
  },
  "Indexer": {
    "PollIntervalSeconds": 4,
    "StartBlock": 0
  },
  "AiGateway": {
    "BaseUrl": "http://localhost:7071",
    "ApiKey": ""
  },
  "Tawheed": {
    "BaseUrl": "http://localhost:8000"
  }
}
EOF
echo "==> Wrote $SETTINGS_FILE"

# ── 8. Print next steps ────────────────────────────────────────────────
cat <<EOF

${BOLD}================================================================================
  HalalChain dev stack is up.${NC}

  Anvil (Polygon fork, chain-id 80002): $ANVIL_RPC
  IPFS kubo API                         : http://127.0.0.1:5011
  IPFS kubo gateway                     : http://127.0.0.1:8081
  Postgres (optional)                    : postgresql://halalchain:halalchain@127.0.0.1:5432/halalchain

  Contract addresses are in: $SETTINGS_FILE

  ${BOLD}Next steps:${NC}

    # Run the API (will pick up the config above automatically)
    cd $REPO_ROOT/HalalChain.Platform.Api
    dotnet run

    # Then in another terminal, register a product on chain
    cast send $ADDR_SUPPLIERS \\
        "registerSupplier(bytes32,address,string,string)" \\
        "0x\$(printf 'SUP-MY-000001' | sha256sum | cut -c1-64)" \\
        0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266 \\
        "ipfs://test" "MY" \\
        --rpc-url $ANVIL_RPC --private-key $PRIVATE_KEY

    # And the public verification endpoint:
    curl -s "http://localhost:5001/api/v1/verify/SUP-MY-000001-PROD-1" | jq .

  To stop everything:  docker compose -f $DOCKER_COMPOSE_FILE --profile chain down
================================================================================${NC}
EOF
