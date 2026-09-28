#!/usr/bin/env bash
set -e
FILE="/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/src/HalalAccessControl.sol"

# Insert requireRole before the final closing brace
# Use awk: when we hit the last }, insert our block first
awk '
/^}$/ && !done {
    print "    // External role-check helper used by sibling contracts"
    print "    /// @notice Reverts if `account` does not hold `role`."
    print "    ///         Mirrors OZ internal _checkRole but callable cross-contract."
    print "    function requireRole(bytes32 role, address account) external view {"
    print "        _checkRole(role, account);"
    print "    }"
    print ""
    done=1
}
{ print }
' "$FILE" > "${FILE}.tmp" && mv "${FILE}.tmp" "$FILE"

echo "HalalAccessControl.sol patched"

# Now replace _checkRole with requireRole in registry files
REGS=(
  "/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/src/SupplierRegistry.sol"
  "/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/src/HalalProductRegistry.sol"
  "/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/src/HalalCertificationRegistry.sol"
)
for f in "${REGS[@]}"; do
  sed -i 's/access\._checkRole(/access.requireRole(/g' "$f"
  echo "Patched: $f"
done